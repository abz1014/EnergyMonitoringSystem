namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData | Columns: kWtotal, MeterNo, DateTime | Derived
// Formula: Diversity Factor = Sum(individual meter max demand) / System coincident max demand
// Assumption: meter timestamps are reasonably synchronized (same polling interval/clock), so
// summing kWtotal across meters at a given timestamp is a valid approximation of the system's
// instantaneous coincident demand. If meters poll at different times, this will understate the
// true coincident peak.
// Validatable against SCADA: indirectly -- can be cross-checked against the gateway's own
// system-level demand reading if one exists, but this app does not currently have access to that.
// Confidence: Medium.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class DiversityFactorController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<DiversityFactorController> _logger;

    public DiversityFactorController(IEnergyMeterRepository meterRepo, IMonitoringDeviceRepository deviceRepo, ILogger<DiversityFactorController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public class MeterPeak
    {
        public int MeterNo { get; set; }
        public string MeterName { get; set; } = "";
        public double MaxDemandKw { get; set; }
        public DateTime? PeakAt { get; set; }
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
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

            var validData = data.Where(d => d.DateTime.HasValue && d.MeterNo.HasValue && d.kWtotal.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            // Need at least 2 meters for diversity factor to be a meaningful concept
            var meterCount = validData.Select(d => d.MeterNo!.Value).Distinct().Count();
            ViewBag.MeterCount = meterCount;
            if (validData.Count == 0 || meterCount < 2)
            {
                ViewBag.HasData = false;
                return View(new List<MeterPeak>());
            }

            var devices = await _deviceRepo.GetAllDevices();
            var deviceNames = devices
                .Where(d => d.DeviceID.HasValue)
                .GroupBy(d => d.DeviceID!.Value)
                .ToDictionary(g => g.Key, g => g.First().DeviceName ?? $"Meter {g.Key}");

            // Individual peak demand per meter
            var meterPeaks = validData
                .GroupBy(d => d.MeterNo!.Value)
                .Select(g =>
                {
                    var peakReading = g.OrderByDescending(d => d.kWtotal!.Value).First();
                    return new MeterPeak
                    {
                        MeterNo = g.Key,
                        MeterName = deviceNames.GetValueOrDefault(g.Key, $"Meter {g.Key}"),
                        MaxDemandKw = Math.Round(peakReading.kWtotal!.Value, 1),
                        PeakAt = peakReading.DateTime
                    };
                })
                .OrderByDescending(m => m.MaxDemandKw)
                .ToList();

            var sumOfIndividualPeaks = meterPeaks.Sum(m => m.MaxDemandKw);

            // System coincident peak: sum kWtotal across all meters at each timestamp, take the max
            var systemDemandByTimestamp = validData
                .GroupBy(d => d.DateTime!.Value)
                .Select(g => new { Timestamp = g.Key, Total = g.Sum(d => d.kWtotal!.Value) })
                .OrderByDescending(x => x.Total)
                .ToList();

            var systemPeak = systemDemandByTimestamp.Count > 0 ? systemDemandByTimestamp.First() : null;
            var systemPeakKw = systemPeak?.Total ?? 0;

            var diversityFactor = systemPeakKw > 0 ? Math.Round(sumOfIndividualPeaks / systemPeakKw, 2) : (double?)null;
            var headroomKw = Math.Max(sumOfIndividualPeaks - systemPeakKw, 0);

            ViewBag.MeterPeaks = meterPeaks;
            ViewBag.SumOfIndividualPeaks = Math.Round(sumOfIndividualPeaks, 1);
            ViewBag.SystemPeakKw = Math.Round(systemPeakKw, 1);
            ViewBag.SystemPeakAt = systemPeak?.Timestamp.ToString("MMM dd, HH:mm");
            ViewBag.DiversityFactor = diversityFactor;
            ViewBag.HeadroomKw = Math.Round(headroomKw, 1);

            return View(meterPeaks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing diversity factor");
            return View("Error");
        }
    }
}
