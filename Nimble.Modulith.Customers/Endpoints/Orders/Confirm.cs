using FastEndpoints;
using Mediator;
using Nimble.Modulith.Customers.Infrastructure;
using Nimble.Modulith.Customers.UseCases.Customers.Queries;
using Nimble.Modulith.Customers.UseCases.Orders.Commands;
using Nimble.Modulith.Customers.UseCases.Orders.Queries;
using Nimble.Modulith.Email.Contracts;

namespace Nimble.Modulith.Customers.Endpoints.Orders;

public class Confirm(IMediator mediator, ICustomerAuthorizationService authService) : EndpointWithoutRequest<OrderResponse>
{
    public override void Configure()
    {
        Post("/orders/{id}/confirm");
        Summary(s =>
        {
            s.Summary = "Confirm an order";
            s.Description = "Changes order status to Processing";
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
        var orderResult = await mediator.Send(new GetOrderByIdQuery(orderId), ct);

        if (!orderResult.IsSuccess)
        {
            AddError($"Order with ID {orderId} not found");
            await Send.ErrorsAsync(statusCode: 404, cancellation: ct);
            return;
        }

        var customerResult = await mediator.Send(new GetCustomerByIdQuery(orderResult.Value.CustomerId), ct);

        if (!customerResult.IsSuccess)
        {
            AddError($"Customer with ID {orderResult.Value.CustomerId} not found");
            await Send.ErrorsAsync(statusCode: 404, cancellation: ct);
            return;
        }

        if (!authService.IsAdminOrOwner(User, customerResult.Value.Email))
        {
            AddError("You can only confirm your own orders");
            await Send.ErrorsAsync(statusCode: 403, cancellation: ct);
            return;
        }

        var result = await mediator.Send(new ConfirmOrderCommand(orderId), ct);

        if (!result.IsSuccess)
        {
            AddError("Failed to confirm order");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var emailBody = $"""
Dear Customer,

Your order has been confirmed!

Order Number: {result.Value.OrderNumber}
Order Date: {result.Value.OrderDate:yyyy-MM-dd}
Total Amount: ${result.Value.TotalAmount:F2}

Items:
{string.Join("\n", result.Value.Items.Select(i => $"- {i.ProductName} x {i.Quantity} @ ${i.UnitPrice:F2} = ${i.TotalPrice:F2}"))}

Thank you for your order!
""";

        await mediator.Send(
            new SendEmailCommand(
                customerResult.Value.Email,
                $"Order Confirmation - {result.Value.OrderNumber}",
                emailBody),
            ct);

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
