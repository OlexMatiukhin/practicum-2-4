namespace Nimble.Modulith.Users.Infrastructure;

public static class PasswordGenerator
{
    public static string GeneratePassword()
    {
        return Guid.NewGuid().ToString("N")[..12];
    }
}
