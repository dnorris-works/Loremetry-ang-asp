namespace backend.Auth;

public static class OperatorAuth
{
    public static RouteGroupBuilder RequireOperator(this RouteGroupBuilder group) =>
        group.AddEndpointFilter(RequireOperatorFilter);

    private static async ValueTask<object?> RequireOperatorFilter(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var authService = httpContext.RequestServices.GetRequiredService<AuthService>();

        try
        {
            var user = await authService.ResolveAsync(
                httpContext.Request.Headers,
                httpContext.RequestAborted);

            if (!user.IsAdmin)
            {
                return Results.Json(
                    new { message = "Operator access required." },
                    statusCode: StatusCodes.Status403Forbidden);
            }
        }
        catch (AuthException exception)
        {
            return Results.Json(
                new { message = exception.Message },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
