namespace backend.Auth;

public sealed class AuthUser
{
    public required long DbUserId { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string ClerkId { get; init; }

    public required bool BreakGlass { get; init; }

    public bool IsAdmin => BreakGlass;
}
