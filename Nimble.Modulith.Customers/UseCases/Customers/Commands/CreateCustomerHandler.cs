using Ardalis.Result;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Nimble.Modulith.Customers.Domain.CustomerAggregate;
using Nimble.Modulith.Customers.Domain.CustomerAggregate.Specifications;
using Nimble.Modulith.Customers.Domain.Interfaces;
using Nimble.Modulith.Email.Contracts;
using Nimble.Modulith.Users.Contracts;

namespace Nimble.Modulith.Customers.UseCases.Customers.Commands;

public class CreateCustomerHandler(
    IRepository<Customer> repository,
    IMediator mediator,
    UserManager<IdentityUser> userManager)
    : ICommandHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    public async ValueTask<Result<CustomerDto>> Handle(CreateCustomerCommand command, CancellationToken ct)
    {
        var existingCustomer = await repository.FirstOrDefaultAsync(new CustomerByEmailSpec(command.Email), ct);
        if (existingCustomer is not null)
        {
            return Result<CustomerDto>.Invalid(new Ardalis.Result.ValidationError
            {
                Identifier = nameof(command.Email),
                ErrorMessage = $"Customer with email '{command.Email}' already exists"
            });
        }

        var existingUser = await userManager.FindByEmailAsync(command.Email);
        string? temporaryPassword = null;

        if (existingUser is null)
        {
            temporaryPassword = Guid.NewGuid().ToString("N")[..12];
            var userResult = await mediator.Send(new CreateUserCommand(command.Email, temporaryPassword), ct);

            if (!userResult.IsSuccess)
            {
                return Result<CustomerDto>.Error($"Failed to create user account: {userResult.Errors.FirstOrDefault()}");
            }
        }

        var customer = new Customer
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            Address = new Address
            {
                Street = command.Street,
                City = command.City,
                State = command.State,
                PostalCode = command.PostalCode,
                Country = command.Country
            }
        };

        await repository.AddAsync(customer, ct);
        await repository.SaveChangesAsync(ct);

        await SendCustomerEmailAsync(command.Email, temporaryPassword, mediator, ct);

        var dto = new CustomerDto(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.PhoneNumber,
            new AddressDto(
                customer.Address.Street,
                customer.Address.City,
                customer.Address.State,
                customer.Address.PostalCode,
                customer.Address.Country
            ),
            customer.CreatedAt,
            customer.UpdatedAt
        );

        return Result<CustomerDto>.Success(dto);
    }

    private static async Task SendCustomerEmailAsync(
        string email,
        string? temporaryPassword,
        IMediator mediator,
        CancellationToken ct)
    {
        if (temporaryPassword is not null)
        {
            var emailBody = $"""
Welcome to our service!

Your account has been created successfully.

Email: {email}
Temporary Password: {temporaryPassword}

Please log in and change your password as soon as possible.

Best regards,
The Team
""";

            await mediator.Send(
                new SendEmailCommand(
                    email,
                    "Welcome - Your Account Has Been Created",
                    emailBody),
                ct);

            return;
        }

        var existingUserEmailBody = $"""
Welcome back!

A customer profile has been created for your existing account.

Email: {email}

You can continue using your existing password to access our services.

Best regards,
The Team
""";

        await mediator.Send(
            new SendEmailCommand(
                email,
                "Customer Profile Created",
                existingUserEmailBody),
            ct);
    }
}
