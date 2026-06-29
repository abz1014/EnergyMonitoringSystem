namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class ReportsController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ReportGeneratorService _reportGenerator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IEnergyMeterRepository meterRepo,
        IMonitoringDeviceRepository deviceRepo,
        ReportGeneratorService reportGenerator,
        ILogger<ReportsController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _reportGenerator = reportGenerator;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? dateFrom = null, string? dateTo = null, int? meterId = null)
    {
        var from = DateTime.TryParse(dateFrom, out var pf1) ? pf1 : DateTime.Now.Date.AddDays(-7);
        var to = DateTime.TryParse(dateTo, out var pt1) ? pt1.AddDays(1).AddTicks(-1) : DateTime.Now.Date.AddDays(1).AddTicks(-1);

        var data = await _meterRepo.GetByDateRange(from, to);
        if (meterId.HasValue)
            data = data.Where(d => d.MeterNo == meterId.Value).ToList();

        var devices = await _deviceRepo.GetAllDevices();
        var distinctDevices = devices.Where(d => d.DeviceID.HasValue && d.IsActive == 1)
            .GroupBy(d => d.DeviceID!.Value).Select(g => g.First()).ToList();

        ViewBag.DateFrom = from.ToString("yyyy-MM-dd");
        ViewBag.DateTo = to.Date.ToString("yyyy-MM-dd");
        ViewBag.SelectedMeterId = meterId;
        ViewBag.Devices = distinctDevices;
        ViewBag.Data = data;
        ViewBag.TotalKwh = data.Sum(d => (double)(d.kWh ?? 0));
        ViewBag.PeakKw = data.Count > 0 ? data.Max(d => d.kWtotal ?? 0) : 0;
        ViewBag.AvgPf = data.Where(d => d.PFL1.HasValue && d.PFL1 > 0).Select(d => d.PFL1!.Value).DefaultIfEmpty(0).Average();
        ViewBag.RecordCount = data.Count;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? dateFrom = null, string? dateTo = null, int? meterId = null)
    {
        var from = DateTime.TryParse(dateFrom, out var pf2) ? pf2 : DateTime.Now.Date.AddDays(-7);
        var to = DateTime.TryParse(dateTo, out var pt2) ? pt2.AddDays(1).AddTicks(-1) : DateTime.Now.Date.AddDays(1).AddTicks(-1);

        var data = await _meterRepo.GetByDateRange(from, to);
        if (meterId.HasValue)
            data = data.Where(d => d.MeterNo == meterId.Value).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("DateTime,MeterNo,MeterName,Location,VoltL1N,VoltL2N,VoltL3N,CurrentL1,CurrentL2,CurrentL3,kWtotal,kVAtotal,kVARtotal,PFL1,MFreq,kWh,kVAh,kVARh");

        foreach (var row in data)
        {
            sb.AppendLine($"{row.DateTime:yyyy-MM-dd HH:mm:ss},{row.MeterNo},{row.MeterName},{row.MeterLocation},{row.VoltL1N},{row.VoltL2N},{row.VoltL3N},{row.CurrentL1},{row.CurrentL2},{row.CurrentL3},{row.kWtotal},{row.kVAtotal},{row.kVARtotal},{row.PFL1},{row.MFreq},{row.kWh},{row.kVAh},{row.kVARh}");
        }

        var fileName = $"EnergyReport_{from:yyyyMMdd}_{to.Date:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string? dateFrom = null, string? dateTo = null, int? meterId = null)
    {
        try
        {
            var from = DateTime.TryParse(dateFrom, out var pFrom) ? pFrom : DateTime.Now.Date.AddDays(-7);
            var to = DateTime.TryParse(dateTo, out var pTo) ? pTo.AddDays(1).AddTicks(-1) : DateTime.Now.Date.AddDays(1).AddTicks(-1);

            var data = await _meterRepo.GetByDateRange(from, to);
            if (meterId.HasValue)
                data = data.Where(d => d.MeterNo == meterId.Value).ToList();

            var devices = await _deviceRepo.GetAllDevices();
            var pdfBytes = _reportGenerator.GenerateReport(from, to.Date, data, devices, meterId);

            var fileName = $"EnergyReport_{from:yyyyMMdd}_{to.Date:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF report");
            return RedirectToAction("Index");
        }
    }
}
