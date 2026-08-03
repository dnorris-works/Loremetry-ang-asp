using backend.Auth;
using backend.Configuration;
using backend.Dtos;

namespace backend.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapGet("/config", GetAuthConfig);
        auth.MapGet("/session", GetAuthSession);

        return app;
    }

    private static IResult GetAuthConfig(ClerkOptions options) =>
        Results.Ok(new AuthConfigDto(options.IsEnabled, options.PublishableKey));

    private static async Task<IResult> GetAuthSession(
        HttpRequest request,
        AuthService authService,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await authService.ResolveAsync(request.Headers, cancellationToken);
            return Results.Ok(new AuthSessionDto(
                Authenticated: true,
                Id: user.DbUserId,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                IsAdmin: user.IsAdmin,
                BreakGlass: user.BreakGlass));
        }
        catch (AuthException exception)
        {
            return Results.Ok(new AuthSessionDto(Authenticated: false, Reason: exception.Message));
        }
    }

}
