using System.Net.Mail;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users");

        users.MapGet("/", GetUsers);
        users.MapGet("/{id:long}", GetUser);
        users.MapPost("/", CreateUser);
        users.MapPut("/{id:long}", UpdateUser);
        users.MapDelete("/{id:long}", DeleteUser);

        return app;
    }

    private static async Task<IResult> GetUsers(AppDbContext db, CancellationToken cancellationToken)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Select(user => ToDto(user))
            .ToListAsync(cancellationToken);

        return Results.Ok(users);
    }

    private static async Task<IResult> GetUser(long id, AppDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(user => ToDto(user))
            .FirstOrDefaultAsync(cancellationToken);

        return user is not null
            ? Results.Ok(user)
            : Results.NotFound(new { message = $"User '{id}' was not found." });
    }

    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUserRequest(request.Email, request.FirstName, request.LastName);
        if (validationError is { } errorMessage)
        {
            return Results.BadRequest(new { message = errorMessage });
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Email = NormalizeEmail(request.Email),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueEmailViolation(exception))
        {
            return Results.Conflict(new { message = $"A user with email '{user.Email}' already exists." });
        }

        return Results.Created($"/api/users/{user.Id}", ToDto(user));
    }

    private static async Task<IResult> UpdateUser(
        long id,
        UpdateUserRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUserRequest(request.Email, request.FirstName, request.LastName);
        if (validationError is { } errorMessage)
        {
            return Results.BadRequest(new { message = errorMessage });
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return Results.NotFound(new { message = $"User '{id}' was not found." });
        }

        user.Email = NormalizeEmail(request.Email);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueEmailViolation(exception))
        {
            return Results.Conflict(new { message = $"A user with email '{user.Email}' already exists." });
        }

        return Results.Ok(ToDto(user));
    }

    private static async Task<IResult> DeleteUser(long id, AppDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return Results.NotFound(new { message = $"User '{id}' was not found." });
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static UserDto ToDto(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.CreatedAt, user.UpdatedAt);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? ValidateUserRequest(string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return "First name is required.";
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return "Last name is required.";
        }

        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            return "Email is not valid.";
        }

        return null;
    }

    private static bool IsUniqueEmailViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}
