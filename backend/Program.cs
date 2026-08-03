using backend.Auth;
using backend.Configuration;
using backend.Data;
using backend.Endpoints;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

EnvFileLoader.LoadFromRepoRoot(builder.Environment.ContentRootPath);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var clerkOptions = ClerkOptions.FromEnvironment();
builder.Services.AddSingleton(clerkOptions);
builder.Services.AddScoped<AuthService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<PlatformConnectionTests>();
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS lore;");
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.users (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            clerk_id VARCHAR(255),
            email VARCHAR(320) NOT NULL,
            first_name VARCHAR(100) NOT NULL DEFAULT '',
            last_name VARCHAR(100) NOT NULL DEFAULT '',
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT users_email_unique UNIQUE (email),
            CONSTRAINT users_clerk_id_unique UNIQUE (clerk_id)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.users ADD COLUMN IF NOT EXISTS clerk_id VARCHAR(255);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE UNIQUE INDEX IF NOT EXISTS users_clerk_id_unique ON lore.users (clerk_id);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.user_settings (
            user_id BIGINT NOT NULL REFERENCES lore.users(id) ON DELETE CASCADE,
            key VARCHAR(120) NOT NULL,
            value TEXT NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            PRIMARY KEY (user_id, key)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.platform_settings (
            key VARCHAR(120) PRIMARY KEY,
            value TEXT NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """);
    await PlatformSettingsService.SeedFromEnvironmentAsync(db, cancellationToken: default);
}

app.UseHttpsRedirection();
app.UseCors();

app.MapHealthChecks("/health");

app.MapGet("/health/db", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(cancellationToken);

    return canConnect switch
    {
        true => Results.Ok(new { status = "connected", database = "postgres" }),
        false => Results.Problem(
            title: "Database unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable),
    };
})
.WithName("GetDatabaseHealth");

app.MapAdminEndpoints();
app.MapPlatformSettingsEndpoints();
app.MapSettingsEndpoints();
app.MapUserEndpoints();
app.MapAuthEndpoints();

app.Run();
