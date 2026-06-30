namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData (kVAtotal) + tblTransformerRatings (NameplateKva, admin-entered)
// Loading % = kVAtotal / NameplateKva * 100
// CRITICAL ASSUMPTION, disclosed on the page: this assumes a 1:1 mapping between each meter
// and a single transformer's secondary -- i.e. the meter is reading the transformer's full
// output, not a sub-feed downstream of it. This has NOT been confirmed against the actual
// electrical single-line diagram. NameplateKva itself is admin-entered, not measured -- it
// must come from the transformer's actual nameplate, not be guessed.
// Validatable against SCADA: kVAtotal yes; NameplateKva no (external data entry).
// Confidence: Medium -- conditional on the 1:1 meter-to-transformer assumption being correct
// and the nameplate values being entered accurately.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class TransformerLoadingController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ScadaDbContext _context;
    private readonly ILogger<TransformerLoadingController> _logger;

    public TransformerLoadingController(IEnergyMeterRepository meterRepo, ScadaDbContext context, ILogger<TransformerLoadingController> logger)
    {
        _meterRepo = meterRepo;
        _context = context;
        _logger = logger;
    }

    public class TransformerStatus
    {
        public int MeterNo { get; set; }
        public string TransformerName { get; set; } = "";
        public decimal NameplateKva { get; set; }
        public string? CoolingClass { get; set; }
        public double PeakKva { get; set; }
        public double AvgKva { get; set; }
        public double PeakLoadingPct { get; set; }
        public double AvgLoadingPct { get; set; }
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "";
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
            var ratings = await _context.TransformerRatings.AsNoTracking().ToListAsync();
            ViewBag.Ratings = ratings;

            var days = range switch { "7d" => 7, "90d" => 90, _ => 30 };
            var to = DateTime.Now.Date.AddDays(1);
            var from = to.AddDays(-days);

            var data = await _meterRepo.GetByDateRange(from, to);
            if (data.Count == 0)
            {
                var recent = await _meterRepo.GetByDateRange(DateTime.Now.AddDays(-60), to);
                if (recent.Count > 0)
                {
                    var latestDate = recent.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    to = latestDate.AddDays(1);
                    from = to.AddDays(-days);
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= from && d.DateTime.Value < to).ToList();
                }
            }

            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            if (ratings.Count == 0)
            {
                ViewBag.HasData = false;
                ViewBag.HasRatings = false;
                return View(new List<TransformerStatus>());
            }
            ViewBag.HasRatings = true;

            var byMeter = data.Where(d => d.MeterNo.HasValue && d.kVAtotal.HasValue).GroupBy(d => d.MeterNo!.Value).ToList();
            var statuses = new List<TransformerStatus>();

            foreach (var rating in ratings)
            {
                var meterData = byMeter.FirstOrDefault(g => g.Key == rating.MeterNo);
                if (meterData == null) continue;

                var kvaValues = meterData.Select(d => d.kVAtotal!.Value).ToList();
                var peakKva = kvaValues.Max();
                var avgKva = kvaValues.Average();
                var peakPct = rating.NameplateKva > 0 ? Math.Round((double)((decimal)peakKva / rating.NameplateKva * 100), 1) : 0;
                var avgPct = rating.NameplateKva > 0 ? Math.Round((double)((decimal)avgKva / rating.NameplateKva * 100), 1) : 0;

                var (status, color) = peakPct switch
                {
                    >= 100 => ("Overloaded", "#EF4444"),
                    >= 80 => ("Near Capacity", "#F59E0B"),
                    _ => ("Healthy", "#10B981")
                };

                statuses.Add(new TransformerStatus
                {
                    MeterNo = rating.MeterNo,
                    TransformerName = rating.TransformerName,
                    NameplateKva = rating.NameplateKva,
                    CoolingClass = rating.CoolingClass,
                    PeakKva = Math.Round(peakKva, 1),
                    AvgKva = Math.Round(avgKva, 1),
                    PeakLoadingPct = peakPct,
                    AvgLoadingPct = avgPct,
                    Status = status,
                    StatusColor = color
                });
            }

            ViewBag.HasData = statuses.Count > 0;
            return View(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transformer loading analysis");
            return View("Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveRating(int meterNo, string transformerName, decimal nameplateKva, string? coolingClass)
    {
        try
        {
            var userName = User.Identity?.Name ?? "unknown";
            var existing = await _context.TransformerRatings.FindAsync(meterNo);
            if (existing != null)
            {
                existing.TransformerName = transformerName;
                existing.NameplateKva = nameplateKva;
                existing.CoolingClass = coolingClass;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedBy = userName;
            }
            else
            {
                _context.TransformerRatings.Add(new TransformerRating
                {
                    MeterNo = meterNo,
                    TransformerName = transformerName,
                    NameplateKva = nameplateKva,
                    CoolingClass = coolingClass,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = userName
                });
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving transformer rating");
        }
        return RedirectToAction("Index");
    }
}
