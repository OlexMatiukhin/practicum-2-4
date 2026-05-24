namespace Nimble.Modulith.Reporting.Services;

public record OrdersReportRow(
    int OrderId,
    string OrderNumber,
    DateTime OrderDate,
    int CustomerId,
    string CustomerEmail,
    decimal TotalAmount);

public record OrdersReportSummary(
    int TotalOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue);

public record OrdersReportResult(
    List<OrdersReportRow> Orders,
    OrdersReportSummary Summary);

public record ProductSalesReportRow(
    int ProductId,
    string ProductName,
    int TotalQuantitySold,
    decimal TotalRevenue,
    int OrderCount);

public record CustomerOrdersReportRow(
    int OrderId,
    string OrderNumber,
    DateTime OrderDate,
    decimal TotalAmount);

public record CustomerOrdersReportSummary(
    int CustomerId,
    string CustomerEmail,
    int TotalOrders,
    decimal TotalSpent,
    DateTime? FirstOrderDate,
    DateTime? LastOrderDate);

public record CustomerOrdersReportResult(
    List<CustomerOrdersReportRow> Orders,
    CustomerOrdersReportSummary Summary);
