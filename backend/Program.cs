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
        CREATE TABLE IF NOT EXISTS lore.app_settings (
            key VARCHAR(120) PRIMARY KEY,
            value TEXT NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.platform_settings (
            key VARCHAR(120) PRIMARY KEY,
            value TEXT NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.stories (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            user_id BIGINT NOT NULL REFERENCES lore.users(id) ON DELETE CASCADE,
            name VARCHAR(200) NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS stories_user_id_updated_at_idx ON lore.stories (user_id, updated_at DESC);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        DROP INDEX IF EXISTS lore.stories_user_id_idx;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.story_documents (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            story_id BIGINT NOT NULL REFERENCES lore.stories(id) ON DELETE CASCADE,
            kind VARCHAR(20) NOT NULL CHECK (kind IN ('manuscript', 'bible')),
            file_name VARCHAR(500) NOT NULL,
            mime_type VARCHAR(127) NOT NULL,
            text_content TEXT,
            binary_content BYTEA,
            sort_order INT NOT NULL DEFAULT 0,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT story_documents_story_kind_filename_unique UNIQUE (story_id, kind, file_name)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS story_documents_story_id_sort_order_idx ON lore.story_documents (story_id, sort_order);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        DROP INDEX IF EXISTS lore.story_documents_story_id_idx;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.series (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            user_id BIGINT NOT NULL REFERENCES lore.users(id) ON DELETE CASCADE,
            name VARCHAR(200) NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS series_user_id_updated_at_idx ON lore.series (user_id, updated_at DESC);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        DROP INDEX IF EXISTS lore.series_user_id_idx;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.series_bible_documents (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            series_id BIGINT NOT NULL REFERENCES lore.series(id) ON DELETE CASCADE,
            file_name VARCHAR(500) NOT NULL,
            mime_type VARCHAR(127) NOT NULL,
            text_content TEXT,
            binary_content BYTEA,
            sort_order INT NOT NULL DEFAULT 0,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT series_bible_documents_series_filename_unique UNIQUE (series_id, file_name)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS series_bible_documents_series_id_sort_order_idx ON lore.series_bible_documents (series_id, sort_order);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        DROP INDEX IF EXISTS lore.series_bible_documents_series_id_idx;
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
app.MapStoryEndpoints();
app.MapSeriesEndpoints();
app.MapAuthEndpoints();

app.Run();
