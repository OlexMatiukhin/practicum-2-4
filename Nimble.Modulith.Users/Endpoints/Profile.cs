using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace Nimble.Modulith.Users.Endpoints;

public class ProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserName { get; set; }
}

public class Profile(UserManager<IdentityUser> userManager) : EndpointWithoutRequest<ProfileResponse>
{
    private readonly UserManager<IdentityUser> _userManager = userManager;

    public override void Configure()
    {
        Get("/profile");
        Summary(s =>
        {
            s.Summary = "View current user profile";
            s.Description = "Returns profile details for the authenticated user";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        Response = new ProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            UserName = user.UserName
        };

        await Send.OkAsync(Response, ct);
    }
}
