namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class EquipmentHealthController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<EquipmentHealthController> _logger;

    public EquipmentHealthController(IEnergyMeterRepository meterRepo, IMonitoringDeviceRepository deviceRepo, ILogger<EquipmentHealthController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public class MeterHealth
    {
        public int MeterNo { get; set; }
        public string MeterName { get; set; } = "";
        public double Score { get; set; }
        public double PfScore { get; set; }
        public double ThdScore { get; set; }
        public double ImbalanceScore { get; set; }
        public double AvgPf { get; set; }
        public double AvgThd { get; set; }
        public double AvgImbalance { get; set; }
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "";
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

            var devices = await _deviceRepo.GetAllDevices();
            var deviceNames = devices
                .Where(d => d.DeviceID.HasValue)
                .GroupBy(d => d.DeviceID!.Value)
                .ToDictionary(g => g.Key, g => g.First().DeviceName ?? $"Meter {g.Key}");

            var byMeter = data.Where(d => d.MeterNo.HasValue).GroupBy(d => d.MeterNo!.Value).ToList();
            ViewBag.HasData = byMeter.Count > 0;
            if (byMeter.Count == 0) return View(new List<MeterHealth>());

            var results = new List<MeterHealth>();

            foreach (var group in byMeter)
            {
                var readings = group.ToList();

                // PF score: 1.0 PF = 100, 0.5 PF = 0 (linear below target)
                var pfValues = readings.Where(d => d.PFL1.HasValue && d.PFL1.Value > 0).Select(d => d.PFL1!.Value).ToList();
                var avgPf = pfValues.Count > 0 ? pfValues.Average() : 0;
                var pfScore = Math.Clamp((avgPf - 0.5) / 0.5 * 100, 0, 100);

                // THD score: using average of HarmonicV1-3 as proxy for THD%. 0% = 100, 8%+ = 0 (IEEE 519 limit)
                var thdValues = readings
                    .Where(d => d.HarmonicV1.HasValue || d.HarmonicV2.HasValue || d.HarmonicV3.HasValue)
                    .Select(d => new[] { d.HarmonicV1, d.HarmonicV2, d.HarmonicV3 }.Where(v => v.HasValue).Select(v => (double)v!.Value).DefaultIfEmpty(0).Average())
                    .ToList();
                var avgThd = thdValues.Count > 0 ? thdValues.Average() : 0;
                var thdScore = Math.Clamp(100 - (avgThd / 8.0 * 100), 0, 100);

                // Imbalance score: 0% = 100, 5%+ = 0 (severe per NEMA)
                var imbalanceValues = readings
                    .Where(d => d.VoltL1N.HasValue && d.VoltL2N.HasValue && d.VoltL3N.HasValue)
                    .Select(d =>
                    {
                        var v1 = d.VoltL1N!.Value; var v2 = d.VoltL2N!.Value; var v3 = d.VoltL3N!.Value;
                        var vMax = Math.Max(v1, Math.Max(v2, v3));
                        var vMin = Math.Min(v1, Math.Min(v2, v3));
                        var vAvg = (v1 + v2 + v3) / 3.0;
                        return vAvg > 0 ? (vMax - vMin) / vAvg * 100.0 : 0;
                    }).ToList();
                var avgImbalance = imbalanceValues.Count > 0 ? imbalanceValues.Average() : 0;
                var imbalanceScore = Math.Clamp(100 - (avgImbalance / 5.0 * 100), 0, 100);

                // Combined score: equal weighting
                var score = (pfScore + thdScore + imbalanceScore) / 3.0;

                var (status, color) = score switch
                {
                    >= 80 => ("Healthy", "#10B981"),
                    >= 60 => ("Fair", "#F59E0B"),
                    _ => ("Needs Attention", "#EF4444")
                };

                results.Add(new MeterHealth
                {
                    MeterNo = group.Key,
                    MeterName = deviceNames.GetValueOrDefault(group.Key, $"Meter {group.Key}"),
                    Score = Math.Round(score, 1),
                    PfScore = Math.Round(pfScore, 1),
                    ThdScore = Math.Round(thdScore, 1),
                    ImbalanceScore = Math.Round(imbalanceScore, 1),
                    AvgPf = Math.Round(avgPf, 3),
                    AvgThd = Math.Round(avgThd, 2),
                    AvgImbalance = Math.Round(avgImbalance, 2),
                    Status = status,
                    StatusColor = color
                });
            }

            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            return View(results.OrderBy(r => r.Score).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading equipment health scores");
            return View("Error");
        }
    }
}
