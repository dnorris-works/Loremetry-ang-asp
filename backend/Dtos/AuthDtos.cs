namespace backend.Dtos;

public record AuthConfigDto(bool ClerkEnabled, string PublishableKey);

public record AuthSessionDto(
    bool Authenticated,
    long? Id = null,
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    bool? IsAdmin = null,
    bool? BreakGlass = null,
    string? Reason = null);
