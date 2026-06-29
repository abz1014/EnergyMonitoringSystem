namespace EMS.Web.Services;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EMS.Core.Models;

public class ReportGeneratorService
{
    private readonly IConfiguration _config;

    public ReportGeneratorService(IConfiguration config)
    {
        _config = config;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateReport(
        DateTime dateFrom, DateTime dateTo,
        List<EnergyMeterData> data,
        List<MonitoringDevice> devices,
        int? meterId)
    {
        var currency = _config.GetValue<string>("TariffRates:Currency") ?? "Rs.";
        var tariffRate = _config.GetValue<double>("TariffRates:DefaultRate", 52.0);

        var totalKwh = data.Sum(d => (double)(d.kWh ?? 0));
        var peakKw = data.Count > 0 ? data.Max(d => d.kWtotal ?? 0) : 0;
        var avgPf = data.Where(d => d.PFL1.HasValue && d.PFL1 > 0).Select(d => d.PFL1!.Value).DefaultIfEmpty(0).Average();
        var totalCost = totalKwh * tariffRate;
        var recordCount = data.Count;

        var deviceLookup = devices.Where(d => d.DeviceID.HasValue)
            .GroupBy(d => d.DeviceID!.Value).ToDictionary(g => g.Key, g => g.First());

        // Daily breakdown
        var dailyData = data.Where(d => d.DateTime.HasValue)
            .GroupBy(d => d.DateTime!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                Kwh = g.Sum(x => (double)(x.kWh ?? 0)),
                PeakKw = g.Max(x => x.kWtotal ?? 0),
                AvgPf = g.Where(x => x.PFL1.HasValue && x.PFL1 > 0).Select(x => x.PFL1!.Value).DefaultIfEmpty(0).Average(),
                Cost = g.Sum(x => (double)(x.kWh ?? 0)) * tariffRate
            })
            .OrderBy(d => d.Date).ToList();

        // Meter breakdown
        var meterData = data.Where(d => d.MeterNo.HasValue)
            .GroupBy(d => d.MeterNo!.Value)
            .Select(g => new
            {
                MeterNo = g.Key,
                Name = g.First().MeterName ?? (deviceLookup.ContainsKey(g.Key) ? deviceLookup[g.Key].DeviceName : $"Meter-{g.Key}") ?? $"Meter-{g.Key}",
                Location = g.First().MeterLocation ?? "",
                Kwh = g.Sum(x => (double)(x.kWh ?? 0)),
                PeakKw = g.Max(x => x.kWtotal ?? 0),
                AvgPf = g.Where(x => x.PFL1.HasValue && x.PFL1 > 0).Select(x => x.PFL1!.Value).DefaultIfEmpty(0).Average(),
                Cost = g.Sum(x => (double)(x.kWh ?? 0)) * tariffRate
            })
            .OrderByDescending(m => m.Kwh).ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Energy Monitoring System").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                            c.Item().Text("Energy Consumption Report").FontSize(12).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text($"{dateFrom:dd MMM yyyy} — {dateTo:dd MMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                });

                page.Content().Column(col =>
                {
                    // Executive Summary
                    col.Item().PaddingBottom(15).Text("Executive Summary").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                    col.Item().PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                        void SummaryCell(string label, string value) =>
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(value).FontSize(14).Bold();
                            });
                        SummaryCell("Total Consumption", $"{totalKwh:N0} kWh");
                        SummaryCell("Peak Demand", $"{peakKw:F1} kW");
                        SummaryCell("Avg Power Factor", $"{avgPf:F3}");
                        SummaryCell("Estimated Cost", $"{currency} {totalCost:N0}");
                        SummaryCell("Records", $"{recordCount:N0}");
                    });

                    // Daily Breakdown
                    if (dailyData.Count > 0)
                    {
                        col.Item().PaddingBottom(10).Text("Daily Breakdown").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingBottom(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1.5f); c.RelativeColumn(2);
                            });

                            // Header
                            void HeaderCell(string t) => table.Cell().Background(Colors.Blue.Darken2).Padding(6).Text(t).FontSize(9).Bold().FontColor(Colors.White);
                            HeaderCell("Date"); HeaderCell("Consumption (kWh)"); HeaderCell("Peak (kW)"); HeaderCell("Avg PF"); HeaderCell($"Cost ({currency})");

                            foreach (var d in dailyData)
                            {
                                var bg = dailyData.IndexOf(d) % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                void DataCell(string t) => table.Cell().Background(bg).Padding(5).Text(t).FontSize(9);
                                DataCell(d.Date.ToString("dd MMM yyyy"));
                                DataCell($"{d.Kwh:N0}");
                                DataCell($"{d.PeakKw:F1}");
                                DataCell($"{d.AvgPf:F3}");
                                DataCell($"{currency} {d.Cost:N0}");
                            }

                            // Total row
                            void TotalCell(string t) => table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(t).FontSize(9).Bold();
                            TotalCell("TOTAL");
                            TotalCell($"{dailyData.Sum(d => d.Kwh):N0}");
                            TotalCell($"{dailyData.Max(d => d.PeakKw):F1}");
                            TotalCell($"{avgPf:F3}");
                            TotalCell($"{currency} {dailyData.Sum(d => d.Cost):N0}");
                        });
                    }

                    // Meter Breakdown
                    if (meterData.Count > 0)
                    {
                        col.Item().PaddingBottom(10).Text("Meter Breakdown").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().PaddingBottom(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2.5f); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1.5f); c.RelativeColumn(1.5f); c.RelativeColumn(2);
                            });

                            void HeaderCell(string t) => table.Cell().Background(Colors.Blue.Darken2).Padding(6).Text(t).FontSize(9).Bold().FontColor(Colors.White);
                            HeaderCell("Meter"); HeaderCell("Location"); HeaderCell("kWh"); HeaderCell("Peak kW"); HeaderCell("Avg PF"); HeaderCell($"Cost ({currency})");

                            foreach (var m in meterData)
                            {
                                var bg = meterData.IndexOf(m) % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                void DataCell(string t) => table.Cell().Background(bg).Padding(5).Text(t).FontSize(9);
                                DataCell(m.Name);
                                DataCell(m.Location);
                                DataCell($"{m.Kwh:N0}");
                                DataCell($"{m.PeakKw:F1}");
                                DataCell($"{m.AvgPf:F3}");
                                DataCell($"{currency} {m.Cost:N0}");
                            }
                        });
                    }

                    // Tariff Note
                    col.Item().PaddingTop(10).Text($"Tariff rate: {currency} {tariffRate}/kWh ({_config.GetValue<string>("TariffRates:TariffCategory") ?? "Commercial"})")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Energy Monitoring System — Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return doc.GeneratePdf();
    }
}
