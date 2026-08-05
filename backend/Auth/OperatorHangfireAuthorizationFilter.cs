using Hangfire.Dashboard;

namespace backend.Auth;

public sealed class OperatorHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        if (environment.IsDevelopment())
        {
            return true;
        }

        var authService = httpContext.RequestServices.GetRequiredService<AuthService>();

        try
        {
            var user = authService
                .ResolveAsync(httpContext.Request.Headers, httpContext.RequestAborted)
                .GetAwaiter()
                .GetResult();
            return user.IsAdmin;
        }
        catch (AuthException)
        {
            return false;
        }
    }
}
