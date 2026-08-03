namespace backend.Auth;

public static class AuthEndpointHelpers
{
    public static async Task<(AuthUser? User, IResult? Error)> TryResolveUserAsync(
        HttpRequest request,
        AuthService authService,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await authService.ResolveAsync(request.Headers, cancellationToken);
            return (user, null);
        }
        catch (AuthException exception)
        {
            return (null, Results.Json(
                new { message = exception.Message },
                statusCode: StatusCodes.Status401Unauthorized));
        }
    }
}
