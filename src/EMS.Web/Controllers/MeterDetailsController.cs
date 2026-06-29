namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Models;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class MeterDetailsController : Controller
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly ILogger<MeterDetailsController> _logger;

    public MeterDetailsController(
        IEnergyMeterRepository energyMeterRepository,
        ILogger<MeterDetailsController> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int meterId, string timeframe = "daily")
    {
        try
        {
            _logger.LogInformation("Meter details requested for MeterId: {MeterId}, timeframe: {timeframe}", meterId, timeframe);

            var (dateFrom, dateTo) = GetDateRange(timeframe);
            var meterData = await _energyMeterRepository.GetByDateRange(dateFrom, dateTo);
            var meterReadings = meterData.Where(m => m.MeterNo == meterId).ToList();

            if (!meterReadings.Any())
            {
                _logger.LogWarning("No data found for MeterId: {MeterId}", meterId);
                return NotFound(new { error = "Meter not found" });
            }

            var latestReading = meterReadings.OrderByDescending(m => m.DateTime).First();
            var stats = CalculateStats(meterReadings);

            var model = new MeterDetailsViewModel
            {
                MeterId = meterId,
                MeterName = latestReading.MeterName ?? $"Meter-{meterId}",
                Timeframe = timeframe,
                DateFrom = dateFrom,
                DateTo = dateTo,
                LastUpdated = latestReading.DateTime ?? DateTime.Now,
                CurrentValues = new MeterReadingViewModel
                {
                    VoltageL1 = (double)(latestReading.VoltL1N ?? 0),
                    VoltageL2 = (double)(latestReading.VoltL2N ?? 0),
                    VoltageL3 = (double)(latestReading.VoltL3N ?? 0),
                    CurrentL1 = (double)(latestReading.CurrentL1 ?? 0),
                    CurrentL2 = (double)(latestReading.CurrentL2 ?? 0),
                    CurrentL3 = (double)(latestReading.CurrentL3 ?? 0),
                    PowerkW = (double)(latestReading.kWtotal ?? 0),
                    PowerFactor = (double)(latestReading.PFL1 ?? 0),
                    Frequency = (double)(latestReading.MFreq ?? 0)
                },
                Statistics = stats,
                TotalConsumption = meterReadings.Sum(m => (double)(m.kWh ?? 0)),
                AverageConsumption = meterReadings.Average(m => (double)(m.kWh ?? 0)),
                PeakConsumption = meterReadings.Max(m => (double)(m.kWh ?? 0)),
                MinConsumption = meterReadings.Min(m => (double)(m.kWh ?? 0))
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading meter details for MeterId: {MeterId}", meterId);
            return View("Error");
        }
    }

    private (DateTime, DateTime) GetDateRange(string timeframe)
    {
        var today = DateTime.Now.Date;
        return timeframe switch
        {
            "daily" => (today, today.AddDays(1).AddTicks(-1)),
            "weekly" => (today.AddDays(-(int)today.DayOfWeek), today.AddDays(7 - (int)today.DayOfWeek).AddTicks(-1)),
            "monthly" => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddTicks(-1)),
            "yearly" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31).AddTicks(-1)),
            _ => (today.AddDays(-30), today.AddDays(1).AddTicks(-1))
        };
    }

    private Dictionary<string, double> CalculateStats(List<EMS.Core.Models.EnergyMeterData> data)
    {
        var values = data.Select(d => (double)(d.kWh ?? 0)).ToList();
        return new()
        {
            { "peak", values.Any() ? values.Max() : 0 },
            { "average", values.Any() ? values.Average() : 0 },
            { "minimum", values.Any() ? values.Min() : 0 },
            { "total", values.Sum() }
        };
    }
}
