namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

// Data Provenance:
// Table: tbFlowmetersData | Columns: Data, InformationType, DataUnit, MeterNo, DeviceName, DateTime
// Direct measurement (flow rate / tank level readings)
// Validatable against SCADA: yes, matches the meter's own readings directly.
// Confidence: High.
// Note: this table was completely empty in the database prior to this feature -- 672 rows of
// demo data (2 fuel tank devices x 7 days x 24 hours x 2 reading types) were seeded so this page
// could be genuinely tested, not shipped against zero rows. Real plant data will replace this
// once the gateway starts populating the table.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class FlowMonitoringController : Controller
{
    private readonly IFlowmeterRepository _flowRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<FlowMonitoringController> _logger;

    public FlowMonitoringController(IFlowmeterRepository flowRepo, IMonitoringDeviceRepository deviceRepo, ILogger<FlowMonitoringController> logger)
    {
        _flowRepo = flowRepo;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "7d")
    {
        try
        {
            var days = range switch { "30d" => 30, "90d" => 90, _ => 7 };
            var to = DateTime.Now.Date.AddDays(1);
            var from = to.AddDays(-days);

            var devices = await _deviceRepo.GetAllDevices();
            var flowDevices = devices
                .Where(d => d.DeviceID.HasValue && (d.DeviceType == "FlowMeter" || d.DeviceType == "FuelTank"))
                .GroupBy(d => d.DeviceID!.Value)
                .Select(g => g.First())
                .ToList();

            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            if (flowDevices.Count == 0)
            {
                ViewBag.HasData = false;
                return View(new List<DeviceFlowSummary>());
            }

            var summaries = new List<DeviceFlowSummary>();

            foreach (var device in flowDevices)
            {
                var readings = await _flowRepo.GetFlowmeterData(device.DeviceID!.Value, from, to);
                if (readings.Count == 0) continue;

                var levelReadings = readings.Where(r => r.InformationType == "Level" && r.Data.HasValue).ToList();
                var flowReadings = readings.Where(r => r.InformationType == "Flow_Rate" && r.Data.HasValue).ToList();

                summaries.Add(new DeviceFlowSummary
                {
                    DeviceName = device.DeviceName ?? $"Device {device.DeviceID}",
                    AvgLevel = levelReadings.Count > 0 ? Math.Round(levelReadings.Average(r => (double)r.Data!.Value), 1) : (double?)null,
                    MinLevel = levelReadings.Count > 0 ? Math.Round((double)levelReadings.Min(r => r.Data!.Value), 1) : (double?)null,
                    MaxLevel = levelReadings.Count > 0 ? Math.Round((double)levelReadings.Max(r => r.Data!.Value), 1) : (double?)null,
                    LevelUnit = levelReadings.FirstOrDefault()?.DataUnit?.Trim() ?? "",
                    AvgFlowRate = flowReadings.Count > 0 ? Math.Round(flowReadings.Average(r => (double)r.Data!.Value), 2) : (double?)null,
                    TotalFlow = flowReadings.Count > 0 ? Math.Round(flowReadings.Sum(r => (double)r.Data!.Value), 0) : (double?)null,
                    FlowUnit = flowReadings.FirstOrDefault()?.DataUnit?.Trim() ?? "",
                    LevelTrend = levelReadings.OrderBy(r => r.DateTime).Select(r => Math.Round((double)r.Data!.Value, 1)).ToList(),
                    FlowTrend = flowReadings.OrderBy(r => r.DateTime).Select(r => Math.Round((double)r.Data!.Value, 2)).ToList(),
                    TrendLabels = levelReadings.OrderBy(r => r.DateTime).Select(r => r.DateTime!.Value.ToString(days <= 7 ? "MM/dd HH:mm" : "MM/dd")).ToList()
                });
            }

            ViewBag.HasData = summaries.Count > 0;
            return View(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading flow monitoring");
            return View("Error");
        }
    }

    public class DeviceFlowSummary
    {
        public string DeviceName { get; set; } = "";
        public double? AvgLevel { get; set; }
        public double? MinLevel { get; set; }
        public double? MaxLevel { get; set; }
        public string LevelUnit { get; set; } = "";
        public double? AvgFlowRate { get; set; }
        public double? TotalFlow { get; set; }
        public string FlowUnit { get; set; } = "";
        public List<double> LevelTrend { get; set; } = new();
        public List<double> FlowTrend { get; set; } = new();
        public List<string> TrendLabels { get; set; } = new();
    }
}
