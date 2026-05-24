using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nimble.Modulith.Customers.Contracts;
using Nimble.Modulith.Reporting.Data;
using Nimble.Modulith.Reporting.Models;

namespace Nimble.Modulith.Reporting.Ingest;

public class OrderCreatedEventHandler(
    ReportingDbContext dbContext,
    ILogger<OrderCreatedEventHandler> logger)
    : INotificationHandler<OrderCreatedEvent>
{
    public async ValueTask Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Ingesting order {OrderNumber} into reporting", notification.OrderNumber);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var dateKey = ConvertToDateKey(notification.OrderDate);
        await EnsureDateAsync(notification.OrderDate, dateKey, cancellationToken);
        await EnsureCustomerAsync(notification, cancellationToken);

        foreach (var item in notification.Items)
        {
            await EnsureProductAsync(item, cancellationToken);

            var exists = await dbContext.FactOrders.AnyAsync(
                f => f.OrderId == notification.OrderId && f.OrderItemId == item.Id,
                cancellationToken);

            if (exists)
            {
                logger.LogInformation(
                    "Skipping duplicate reporting fact for order {OrderId}, item {OrderItemId}",
                    notification.OrderId,
                    item.Id);
                continue;
            }

            dbContext.FactOrders.Add(new FactOrder
            {
                OrderId = notification.OrderId,
                OrderItemId = item.Id,
                OrderNumber = notification.OrderNumber,
                DateKey = dateKey,
                CustomerId = notification.CustomerId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                OrderTotalAmount = notification.TotalAmount
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Order {OrderNumber} ingested into reporting", notification.OrderNumber);
    }

    private async Task EnsureDateAsync(DateOnly orderDate, int dateKey, CancellationToken ct)
    {
        if (await dbContext.DimDates.AnyAsync(d => d.DateKey == dateKey, ct))
        {
            return;
        }

        var date = orderDate.ToDateTime(TimeOnly.MinValue);
        dbContext.DimDates.Add(new DimDate
        {
            DateKey = dateKey,
            Date = date,
            Year = date.Year,
            Quarter = (date.Month - 1) / 3 + 1,
            Month = date.Month,
            Day = date.Day,
            DayOfWeek = (int)date.DayOfWeek,
            DayName = date.DayOfWeek.ToString(),
            MonthName = date.ToString("MMMM")
        });
    }

    private async Task EnsureCustomerAsync(OrderCreatedEvent notification, CancellationToken ct)
    {
        var customer = await dbContext.DimCustomers.FindAsync(new object[] { notification.CustomerId }, ct);
        if (customer is null)
        {
            dbContext.DimCustomers.Add(new DimCustomer
            {
                CustomerId = notification.CustomerId,
                Email = notification.CustomerEmail
            });
            return;
        }

        customer.Email = notification.CustomerEmail;
    }

    private async Task EnsureProductAsync(OrderItemDetails item, CancellationToken ct)
    {
        var product = await dbContext.DimProducts.FindAsync(new object[] { item.ProductId }, ct);
        if (product is null)
        {
            dbContext.DimProducts.Add(new DimProduct
            {
                ProductId = item.ProductId,
                Name = item.ProductName
            });
            return;
        }

        product.Name = item.ProductName;
    }

    private static int ConvertToDateKey(DateOnly date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }
}
