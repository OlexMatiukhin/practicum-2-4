using FastEndpoints;
using Mediator;
using Nimble.Modulith.Customers.Infrastructure;
using Nimble.Modulith.Customers.UseCases.Customers.Queries;
using Nimble.Modulith.Customers.UseCases.Orders.Commands;
using Nimble.Modulith.Customers.UseCases.Orders.Queries;

namespace Nimble.Modulith.Customers.Endpoints.Orders;

public class DeleteItem(IMediator mediator, ICustomerAuthorizationService authService) : EndpointWithoutRequest<OrderResponse>
{
    public override void Configure()
    {
        Delete("/orders/{id}/items/{itemId}");
        Summary(s =>
        {
            s.Summary = "Delete an item from an order";
            s.Description = "Removes an item from an existing order";
        });
        Tags("orders");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var orderId = Route<int>("id");
        var itemId = Route<int>("itemId");

        var orderQuery = new GetOrderByIdQuery(orderId);
        var orderResult = await mediator.Send(orderQuery, ct);

        if (!orderResult.IsSuccess)
        {
            AddError($"Order with ID {orderId} not found");
            await Send.ErrorsAsync(statusCode: 404, cancellation: ct);
            return;
        }

        var customerQuery = new GetCustomerByIdQuery(orderResult.Value.CustomerId);
        var customerResult = await mediator.Send(customerQuery, ct);

        if (customerResult.IsSuccess && !authService.IsAdminOrOwner(User, customerResult.Value.Email))
        {
            AddError("You can only modify your own orders");
            await Send.ErrorsAsync(statusCode: 403, cancellation: ct);
            return;
        }

        var command = new DeleteOrderItemCommand(orderId, itemId);

        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        Response = new OrderResponse(
            result.Value.Id,
            result.Value.CustomerId,
            result.Value.OrderNumber,
            result.Value.OrderDate,
            result.Value.Status,
            result.Value.TotalAmount,
            result.Value.Items.Select(i => new OrderItemResponse(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            )).ToList()
        );
    }
}
