namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class DataQualityController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<DataQualityController> _logger;

    public DataQualityController(IEnergyMeterRepository meterRepo, IMonitoringDeviceRepository deviceRepo, ILogger<DataQualityController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public class MeterQuality
    {
        public int MeterNo { get; set; }
        public string MeterName { get; set; } = "";
        public int ExpectedReadings { get; set; }
        public int ActualReadings { get; set; }
        public double Score { get; set; }
        public List<string> GapPeriods { get; set; } = new();
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "";
    }

    public async Task<IActionResult> Index(string range = "7d")
    {
        try
        {
            var days = range switch { "30d" => 30, "90d" => 90, _ => 7 };
            var to = DateTime.Now.Date.AddDays(1);
            var from = to.AddDays(-days);

            var data = await _meterRepo.GetByDateRange(from, to);

            // Anchor to actual data range if "now" has no recent data, so the quality score
            // reflects real reporting gaps rather than penalizing for a period with no deployment yet
            if (data.Count == 0)
            {
                var recent = await _meterRepo.GetByDateRange(DateTime.Now.AddDays(-90), to);
                if (recent.Count > 0)
                {
                    var latestDate = recent.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    to = latestDate.AddDays(1);
                    from = to.AddDays(-days);
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= from && d.DateTime.Value < to).ToList();
                }
            }

            ViewBag.HasData = data.Count > 0;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            if (data.Count == 0) return View(new List<MeterQuality>());

            var devices = await _deviceRepo.GetAllDevices();
            var deviceNames = devices
                .Where(d => d.DeviceID.HasValue)
                .GroupBy(d => d.DeviceID!.Value)
                .ToDictionary(g => g.Key, g => g.First().DeviceName ?? $"Meter {g.Key}");

            // Expected cadence: one reading per hour (matches the seeded demo data interval)
            var expectedHours = (int)(to - from).TotalHours;

            var byMeter = data.Where(d => d.MeterNo.HasValue && d.DateTime.HasValue).GroupBy(d => d.MeterNo!.Value).ToList();
            var allMeterNos = deviceNames.Keys.Where(k => deviceNames[k].Contains("Meter")).ToList();
            // Fall back to whatever meter numbers actually appear in data if device names don't disambiguate
            if (allMeterNos.Count == 0) allMeterNos = byMeter.Select(g => g.Key).ToList();

            var results = new List<MeterQuality>();

            foreach (var meterNo in allMeterNos.Union(byMeter.Select(g => g.Key)).Distinct())
            {
                var readings = byMeter.FirstOrDefault(g => g.Key == meterNo)?.ToList() ?? new List<EMS.Core.Models.EnergyMeterData>();
                var actualHours = readings.Select(d => new DateTime(d.DateTime!.Value.Year, d.DateTime.Value.Month, d.DateTime.Value.Day, d.DateTime.Value.Hour, 0, 0)).Distinct().OrderBy(h => h).ToList();
                var score = expectedHours > 0 ? Math.Round((double)actualHours.Count / expectedHours * 100, 1) : 0;

                // Find gap periods (consecutive missing hours)
                var gaps = new List<string>();
                if (actualHours.Count > 0)
                {
                    var actualSet = actualHours.ToHashSet();
                    DateTime? gapStart = null;
                    var cursor = from;
                    while (cursor < to)
                    {
                        if (!actualSet.Contains(cursor))
                        {
                            gapStart ??= cursor;
                        }
                        else if (gapStart.HasValue)
                        {
                            gaps.Add(FormatGap(gapStart.Value, cursor));
                            gapStart = null;
                        }
                        cursor = cursor.AddHours(1);
                    }
                    if (gapStart.HasValue) gaps.Add(FormatGap(gapStart.Value, to));
                }

                var (status, color) = score switch
                {
                    >= 95 => ("Excellent", "#10B981"),
                    >= 80 => ("Good", "#3B82F6"),
                    >= 50 => ("Fair", "#F59E0B"),
                    _ => ("Poor", "#EF4444")
                };

                results.Add(new MeterQuality
                {
                    MeterNo = meterNo,
                    MeterName = deviceNames.GetValueOrDefault(meterNo, $"Meter {meterNo}"),
                    ExpectedReadings = expectedHours,
                    ActualReadings = actualHours.Count,
                    Score = score,
                    GapPeriods = gaps.Take(10).ToList(),
                    Status = status,
                    StatusColor = color
                });
            }

            return View(results.OrderBy(r => r.Score).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data quality score");
            return View("Error");
        }
    }

    private static string FormatGap(DateTime start, DateTime end)
    {
        var hours = (end - start).TotalHours;
        return hours <= 1 ? start.ToString("MMM dd HH:mm") : $"{start:MMM dd HH:mm} — {end:MMM dd HH:mm} ({hours:F0}h)";
    }
}
