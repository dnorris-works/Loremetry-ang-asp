namespace backend.Dtos;

public record UserDto(
    long Id,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateUserRequest(string Email, string FirstName, string LastName);

public record UpdateUserRequest(string Email, string FirstName, string LastName);
