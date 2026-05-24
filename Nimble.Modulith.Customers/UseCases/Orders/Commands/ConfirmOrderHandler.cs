using Ardalis.Result;
using Mediator;
using Nimble.Modulith.Customers.Contracts;
using Nimble.Modulith.Customers.Domain.CustomerAggregate;
using Nimble.Modulith.Customers.Domain.Interfaces;
using Nimble.Modulith.Customers.Domain.OrderAggregate;

namespace Nimble.Modulith.Customers.UseCases.Orders.Commands;

public class ConfirmOrderHandler(
    IRepository<Order> repository,
    IReadRepository<Customer> customerRepository,
    IMediator mediator)
    : ICommandHandler<ConfirmOrderCommand, Result<OrderDto>>
{
    public async ValueTask<Result<OrderDto>> Handle(ConfirmOrderCommand command, CancellationToken ct)
    {
        var spec = new OrderByIdSpec(command.OrderId);
        var order = await repository.SingleOrDefaultAsync(spec, ct);

        if (order is null)
        {
            return Result<OrderDto>.NotFound($"Order with ID {command.OrderId} not found");
        }

        var customer = await customerRepository.GetByIdAsync(order.CustomerId, ct);
        if (customer is null)
        {
            return Result<OrderDto>.NotFound($"Customer with ID {order.CustomerId} not found");
        }

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(order, ct);
        await repository.SaveChangesAsync(ct);

        var dto = ToDto(order);
        var orderCreatedEvent = new OrderCreatedEvent(
            dto.Id,
            dto.CustomerId,
            customer.Email,
            dto.OrderNumber,
            dto.OrderDate,
            dto.TotalAmount,
            dto.Items.Select(i => new OrderItemDetails(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            )).ToList()
        );

        await mediator.Publish(orderCreatedEvent, ct);

        return Result<OrderDto>.Success(dto);
    }

    private static OrderDto ToDto(Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.OrderNumber,
            order.OrderDate,
            order.Status.ToString(),
            order.TotalAmount,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            )).ToList(),
            order.CreatedAt,
            order.UpdatedAt
        );
}
