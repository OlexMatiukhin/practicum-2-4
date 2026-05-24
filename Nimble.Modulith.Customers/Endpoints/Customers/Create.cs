using FastEndpoints;
using Mediator;
using Nimble.Modulith.Customers.Infrastructure;
using Nimble.Modulith.Customers.UseCases.Customers.Commands;

namespace Nimble.Modulith.Customers.Endpoints.Customers;

public class Create(IMediator mediator, ICustomerAuthorizationService authService) : Endpoint<CreateCustomerRequest, CustomerResponse>
{
    public override void Configure()
    {
        Post("/customers");
        Summary(s =>
        {
            s.Summary = "Create a new customer";
            s.Description = "Creates a new customer with the provided information";
        });
        Tags("customers");
    }

    public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!authService.IsAdminOrOwner(User, req.Email))
        {
            AddError("You can only create a customer record for your own email address");
            await Send.ErrorsAsync(statusCode: 403, cancellation: ct);
            return;
        }

        var command = new CreateCustomerCommand(
            req.FirstName,
            req.LastName,
            req.Email,
            req.PhoneNumber,
            req.Address.Street,
            req.Address.City,
            req.Address.State,
            req.Address.PostalCode,
            req.Address.Country
        );

        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            if (result.Status == Ardalis.Result.ResultStatus.Invalid)
            {
                foreach (var error in result.ValidationErrors)
                {
                    AddError(error.ErrorMessage);
                }

                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            await Send.NotFoundAsync(ct);
            return;
        }

        Response = new CustomerResponse(
            result.Value.Id,
            result.Value.FirstName,
            result.Value.LastName,
            result.Value.Email,
            result.Value.PhoneNumber,
            new AddressResponse(
                result.Value.Address.Street,
                result.Value.Address.City,
                result.Value.Address.State,
                result.Value.Address.PostalCode,
                result.Value.Address.Country
            )
        );

        await Send.CreatedAtAsync<GetById>(new { id = result.Value.Id }, generateAbsoluteUrl: false, cancellation: ct);
    }
}
