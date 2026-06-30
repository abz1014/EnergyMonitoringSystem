namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData | Column: kVAtotal | Direct measurement
// Setting: Tariff.ContractedDemandKva (admin-entered, persisted) -- the utility-contracted
// maximum demand. This is NOT measured -- it's a billing/contract constant that must be entered
// accurately from the actual utility agreement, not estimated.
// System kVA at each timestamp = sum of kVAtotal across all meters at that timestamp (same
// coincidence assumption disclosed on the Diversity Factor page: meters must share timestamps
// for this to reflect true system demand).
// Validatable against SCADA: kVAtotal yes; ContractedDemandKva is an external billing constant.
// Confidence: Medium -- conditional on the contracted value being entered correctly and meter
// timestamp synchronization.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class CapacityHeadroomController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<CapacityHeadroomController> _logger;

    public CapacityHeadroomController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<CapacityHeadroomController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
            var contractedKva = await _settings.GetDoubleAsync("Tariff.ContractedDemandKva", 130.0);

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

            var validData = data.Where(d => d.DateTime.HasValue && d.kVAtotal.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;
            ViewBag.Range = range;
            ViewBag.ContractedKva = contractedKva;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            if (validData.Count == 0) return View();

            // System kVA at each shared timestamp
            var systemKvaByTimestamp = validData
                .GroupBy(d => d.DateTime!.Value)
                .Select(g => new { Timestamp = g.Key, Total = g.Sum(d => d.kVAtotal!.Value) })
                .OrderBy(x => x.Timestamp)
                .ToList();

            var peakEntry = systemKvaByTimestamp.OrderByDescending(x => x.Total).First();
            var avgKva = systemKvaByTimestamp.Average(x => x.Total);
            var peakHeadroomKva = contractedKva - peakEntry.Total;
            var peakUtilizationPct = contractedKva > 0 ? Math.Round(peakEntry.Total / contractedKva * 100, 1) : 0;

            var exceedances = systemKvaByTimestamp.Count(x => x.Total > contractedKva);
            var exceedancePct = Math.Round((double)exceedances / systemKvaByTimestamp.Count * 100, 2);

            ViewBag.PeakKva = Math.Round(peakEntry.Total, 1);
            ViewBag.PeakAt = peakEntry.Timestamp.ToString("MMM dd, HH:mm");
            ViewBag.AvgKva = Math.Round(avgKva, 1);
            ViewBag.PeakHeadroomKva = Math.Round(peakHeadroomKva, 1);
            ViewBag.PeakUtilizationPct = peakUtilizationPct;
            ViewBag.Exceedances = exceedances;
            ViewBag.ExceedancePct = exceedancePct;
            ViewBag.TotalReadings = systemKvaByTimestamp.Count;

            var sampleEvery = Math.Max(1, systemKvaByTimestamp.Count / 300);
            var sampled = systemKvaByTimestamp.Where((x, i) => i % sampleEvery == 0).ToList();
            ViewBag.ChartLabels = JsonSerializer.Serialize(sampled.Select(x => x.Timestamp.ToString(days <= 7 ? "MM/dd HH:mm" : "MM/dd")));
            ViewBag.ChartValues = JsonSerializer.Serialize(sampled.Select(x => Math.Round(x.Total, 1)));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading capacity headroom analysis");
            return View("Error");
        }
    }
}
