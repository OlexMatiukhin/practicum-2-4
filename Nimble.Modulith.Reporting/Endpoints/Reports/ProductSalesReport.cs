using FastEndpoints;
using Nimble.Modulith.Reporting.Endpoints;
using Nimble.Modulith.Reporting.Services;

namespace Nimble.Modulith.Reporting.Endpoints.Reports;

public class ProductSalesReportRequest
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Format { get; set; }
}

public class ProductSalesReport(IReportService reportService) : Endpoint<ProductSalesReportRequest>
{
    public override void Configure()
    {
        Get("/reports/product-sales");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Product sales report";
            s.Description = "Returns product sales ranked by revenue";
        });
        Tags("reports");
    }

    public override async Task HandleAsync(ProductSalesReportRequest req, CancellationToken ct)
    {
        var result = await reportService.GetProductSalesReportAsync(req.StartDate, req.EndDate, ct);

        if (WantsCsv(req.Format))
        {
            await Send.StringAsync(CsvFormatter.Format(result), contentType: "text/csv", cancellation: ct);
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
