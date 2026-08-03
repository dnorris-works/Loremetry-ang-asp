using backend.Data;
using backend.Endpoints;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS lore;");
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.app_settings (
            key VARCHAR(120) PRIMARY KEY,
            value TEXT NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        INSERT INTO lore.app_settings (key, value, updated_at)
        VALUES ('theme', 'light', NOW())
        ON CONFLICT (key) DO NOTHING;
        """);

    if (app.Environment.IsDevelopment())
    {
        await db.Database.EnsureCreatedAsync();
    }
}

app.UseHttpsRedirection();
app.UseCors();

app.MapHealthChecks("/health");

app.MapGet("/health/db", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(cancellationToken);

    return canConnect
        ? Results.Ok(new { status = "connected", database = "postgres" })
        : Results.Problem(
            title: "Database unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable);
})
.WithName("GetDatabaseHealth");

app.MapAdminEndpoints();
app.MapSettingsEndpoints();

app.Run();
