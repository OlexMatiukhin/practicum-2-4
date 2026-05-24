using FastEndpoints;
using Nimble.Modulith.Reporting.Endpoints;
using Nimble.Modulith.Reporting.Services;

namespace Nimble.Modulith.Reporting.Endpoints.Reports;

public class OrdersReportRequest
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Format { get; set; }
}

public class OrdersReport(IReportService reportService) : Endpoint<OrdersReportRequest>
{
    public override void Configure()
    {
        Get("/reports/orders");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Orders report";
            s.Description = "Returns order totals and summary statistics";
        });
        Tags("reports");
    }

    public override async Task HandleAsync(OrdersReportRequest req, CancellationToken ct)
    {
        var result = await reportService.GetOrdersReportAsync(req.StartDate, req.EndDate, ct);

        if (WantsCsv(req.Format))
        {
            await Send.StringAsync(CsvFormatter.Format(result.Orders), contentType: "text/csv", cancellation: ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }

    private bool WantsCsv(string? format)
    {
        return string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
            || HttpContext.Request.Headers.Accept.Any(h => h?.Contains("text/csv", StringComparison.OrdinalIgnoreCase) == true);
    }
}
