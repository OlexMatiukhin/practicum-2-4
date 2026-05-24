namespace Nimble.Modulith.Reporting.Services;

public interface IReportService
{
    Task<OrdersReportResult> GetOrdersReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);
    Task<List<ProductSalesReportRow>> GetProductSalesReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);
    Task<CustomerOrdersReportResult> GetCustomerOrdersReportAsync(int customerId, CancellationToken ct);
}
