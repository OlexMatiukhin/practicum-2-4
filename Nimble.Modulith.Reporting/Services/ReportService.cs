using Dapper;
using Microsoft.EntityFrameworkCore;
using Nimble.Modulith.Reporting.Data;

namespace Nimble.Modulith.Reporting.Services;

public class ReportService(ReportingDbContext dbContext) : IReportService
{
    public async Task<OrdersReportResult> GetOrdersReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();
        var startKey = ToDateKey(startDate);
        var endKey = ToDateKey(endDate);

        const string ordersSql = """
SELECT
    f.OrderId,
    f.OrderNumber,
    MIN(d.[Date]) AS OrderDate,
    f.CustomerId,
    MAX(c.Email) AS CustomerEmail,
    MAX(f.OrderTotalAmount) AS TotalAmount
FROM Reporting.FactOrders f
JOIN Reporting.DimDate d ON d.DateKey = f.DateKey
JOIN Reporting.DimCustomer c ON c.CustomerId = f.CustomerId
WHERE f.DateKey BETWEEN @StartKey AND @EndKey
GROUP BY f.OrderId, f.OrderNumber, f.CustomerId
ORDER BY MIN(d.[Date]), f.OrderNumber;
""";

        var orders = (await connection.QueryAsync<OrdersReportRow>(
            new CommandDefinition(ordersSql, new { StartKey = startKey, EndKey = endKey }, cancellationToken: ct))).ToList();

        var summary = new OrdersReportSummary(
            orders.Count,
            orders.Sum(o => o.TotalAmount),
            orders.Count == 0 ? 0 : orders.Average(o => o.TotalAmount));

        return new OrdersReportResult(orders, summary);
    }

    public async Task<List<ProductSalesReportRow>> GetProductSalesReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();
        var startKey = ToDateKey(startDate);
        var endKey = ToDateKey(endDate);

        const string sql = """
SELECT
    p.ProductId,
    p.Name AS ProductName,
    SUM(f.Quantity) AS TotalQuantitySold,
    SUM(f.TotalPrice) AS TotalRevenue,
    COUNT(DISTINCT f.OrderId) AS OrderCount
FROM Reporting.FactOrders f
JOIN Reporting.DimProduct p ON p.ProductId = f.ProductId
WHERE f.DateKey BETWEEN @StartKey AND @EndKey
GROUP BY p.ProductId, p.Name
ORDER BY SUM(f.TotalPrice) DESC;
""";

        return (await connection.QueryAsync<ProductSalesReportRow>(
            new CommandDefinition(sql, new { StartKey = startKey, EndKey = endKey }, cancellationToken: ct))).ToList();
    }

    public async Task<CustomerOrdersReportResult> GetCustomerOrdersReportAsync(int customerId, CancellationToken ct)
    {
        var connection = dbContext.Database.GetDbConnection();

        const string ordersSql = """
SELECT
    f.OrderId,
    f.OrderNumber,
    MIN(d.[Date]) AS OrderDate,
    MAX(f.OrderTotalAmount) AS TotalAmount
FROM Reporting.FactOrders f
JOIN Reporting.DimDate d ON d.DateKey = f.DateKey
WHERE f.CustomerId = @CustomerId
GROUP BY f.OrderId, f.OrderNumber
ORDER BY MIN(d.[Date]), f.OrderNumber;
""";

        var orders = (await connection.QueryAsync<CustomerOrdersReportRow>(
            new CommandDefinition(ordersSql, new { CustomerId = customerId }, cancellationToken: ct))).ToList();

        const string customerSql = """
SELECT TOP 1 CustomerId, Email
FROM Reporting.DimCustomer
WHERE CustomerId = @CustomerId;
""";

        var customer = await connection.QueryFirstOrDefaultAsync<(int CustomerId, string Email)>(
            new CommandDefinition(customerSql, new { CustomerId = customerId }, cancellationToken: ct));

        var summary = new CustomerOrdersReportSummary(
            customerId,
            customer.Email ?? string.Empty,
            orders.Count,
            orders.Sum(o => o.TotalAmount),
            orders.Count == 0 ? null : orders.Min(o => o.OrderDate),
            orders.Count == 0 ? null : orders.Max(o => o.OrderDate));

        return new CustomerOrdersReportResult(orders, summary);
    }

    private static int ToDateKey(DateOnly date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }
}
