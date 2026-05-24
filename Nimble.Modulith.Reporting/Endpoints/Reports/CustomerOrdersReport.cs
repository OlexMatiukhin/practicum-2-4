using FastEndpoints;
using Nimble.Modulith.Reporting.Endpoints;
using Nimble.Modulith.Reporting.Services;

namespace Nimble.Modulith.Reporting.Endpoints.Reports;

public class CustomerOrdersReportRequest
{
    public int CustomerId { get; set; }
    public string? Format { get; set; }
}

public class CustomerOrdersReport(IReportService reportService) : Endpoint<CustomerOrdersReportRequest>
{
    public override void Configure()
    {
        Get("/reports/customers/{customerId}/orders");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Customer orders report";
            s.Description = "Returns all orders and lifetime metrics for a customer";
        });
        Tags("reports");
    }

    public override async Task HandleAsync(CustomerOrdersReportRequest req, CancellationToken ct)
    {
        var result = await reportService.GetCustomerOrdersReportAsync(req.CustomerId, ct);

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
