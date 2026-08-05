using backend.Auth;
using backend.Configuration;
using backend.Data;
using backend.Endpoints;
using backend.Jobs;
using backend.Services;
using Hangfire;
using Hangfire.PostgreSql;
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
builder.Services.AddScoped<TokenMixCompletionService>();
builder.Services.AddScoped<TokenMixPricingSyncService>();
builder.Services.AddScoped<TokenMixPricingSyncJob>();
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(
        options => options.UseNpgsqlConnection(connectionString),
        new PostgreSqlStorageOptions
        {
            SchemaName = "hangfire",
        }));
builder.Services.AddHangfireServer();
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS lore;");
    await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS hangfire;");
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
            kind VARCHAR(20) NOT NULL CHECK (kind IN ('manuscript', 'character', 'location')),
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
        UPDATE lore.story_documents SET kind = 'character' WHERE kind = 'bible';
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.story_documents DROP CONSTRAINT IF EXISTS story_documents_kind_check;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.story_documents
            ADD CONSTRAINT story_documents_kind_check
            CHECK (kind IN ('manuscript', 'character', 'location'));
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
        ALTER TABLE lore.stories ADD COLUMN IF NOT EXISTS series_id BIGINT REFERENCES lore.series(id) ON DELETE SET NULL;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.stories ADD COLUMN IF NOT EXISTS series_sort_order INT NOT NULL DEFAULT 0;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        WITH ranked AS (
            SELECT
                id,
                ROW_NUMBER() OVER (
                    PARTITION BY series_id
                    ORDER BY updated_at ASC, id ASC
                ) - 1 AS sort_order
            FROM lore.stories
            WHERE series_id IS NOT NULL
        )
        UPDATE lore.stories AS story
        SET series_sort_order = ranked.sort_order
        FROM ranked
        WHERE story.id = ranked.id;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS stories_series_id_sort_order_idx ON lore.stories (series_id, series_sort_order);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        DROP INDEX IF EXISTS lore.stories_series_id_idx;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.series_bible_documents (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            series_id BIGINT NOT NULL REFERENCES lore.series(id) ON DELETE CASCADE,
            category VARCHAR(20) NOT NULL DEFAULT 'character' CHECK (category IN ('character', 'location')),
            file_name VARCHAR(500) NOT NULL,
            mime_type VARCHAR(127) NOT NULL,
            text_content TEXT,
            binary_content BYTEA,
            sort_order INT NOT NULL DEFAULT 0,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT series_bible_documents_series_category_filename_unique UNIQUE (series_id, category, file_name)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.series_bible_documents ADD COLUMN IF NOT EXISTS category VARCHAR(20) NOT NULL DEFAULT 'character';
        """);
    await db.Database.ExecuteSqlRawAsync("""
        UPDATE lore.series_bible_documents SET category = 'character' WHERE category IS NULL OR category = '';
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.series_bible_documents DROP CONSTRAINT IF EXISTS series_bible_documents_category_check;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.series_bible_documents
            ADD CONSTRAINT series_bible_documents_category_check
            CHECK (category IN ('character', 'location'));
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.series_bible_documents DROP CONSTRAINT IF EXISTS series_bible_documents_series_filename_unique;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.series_bible_documents
            DROP CONSTRAINT IF EXISTS series_bible_documents_series_category_filename_unique;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE lore.series_bible_documents
            ADD CONSTRAINT series_bible_documents_series_category_filename_unique
            UNIQUE (series_id, category, file_name);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS series_bible_documents_series_id_sort_order_idx ON lore.series_bible_documents (series_id, sort_order);
        """);
    await db.Database.ExecuteSqlRawAsync("""
        DROP INDEX IF EXISTS lore.series_bible_documents_series_id_idx;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.document_types (
            code VARCHAR(20) PRIMARY KEY,
            display_name VARCHAR(100) NOT NULL,
            sort_order INT NOT NULL DEFAULT 0,
            applies_to_story BOOLEAN NOT NULL DEFAULT TRUE,
            applies_to_series BOOLEAN NOT NULL DEFAULT FALSE,
            active BOOLEAN NOT NULL DEFAULT TRUE
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        INSERT INTO lore.document_types (code, display_name, sort_order, applies_to_story, applies_to_series, active)
        VALUES
            ('manuscript', 'Chapter', 1, TRUE, FALSE, TRUE),
            ('character', 'Character', 2, TRUE, TRUE, TRUE),
            ('location', 'Location', 3, TRUE, TRUE, TRUE)
        ON CONFLICT (code) DO NOTHING;
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.kdp_categories (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            path TEXT NOT NULL,
            store TEXT NOT NULL DEFAULT 'Kindle',
            amazon_node_id TEXT,
            source TEXT NOT NULL DEFAULT 'manual',
            verified_at TEXT,
            created_at TEXT NOT NULL DEFAULT NOW()::text,
            last_seen_at TEXT,
            CONSTRAINT kdp_categories_path_store_unique UNIQUE (path, store)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.writing_drafts (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            user_id BIGINT NOT NULL REFERENCES lore.users(id) ON DELETE CASCADE,
            draft_key VARCHAR(500) NOT NULL,
            mode VARCHAR(20) NOT NULL,
            source VARCHAR(20),
            parent_id BIGINT,
            document_id BIGINT,
            category VARCHAR(20) NOT NULL DEFAULT '',
            title VARCHAR(500) NOT NULL DEFAULT '',
            file_name VARCHAR(500) NOT NULL DEFAULT '',
            text_content TEXT NOT NULL DEFAULT '',
            destination_key VARCHAR(120) NOT NULL DEFAULT '',
            mime_type VARCHAR(127) NOT NULL DEFAULT '',
            updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT writing_drafts_user_key_unique UNIQUE (user_id, draft_key)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.provider_models (
            id VARCHAR(200) NOT NULL,
            provider VARCHAR(50) NOT NULL DEFAULT 'tokenmix',
            owned_by VARCHAR(100) NOT NULL DEFAULT '',
            display_name VARCHAR(200) NOT NULL DEFAULT '',
            model_type VARCHAR(50) NOT NULL DEFAULT '',
            input_price DOUBLE PRECISION,
            output_price DOUBLE PRECISION,
            input_price_unit VARCHAR(20) NOT NULL DEFAULT 'per_million',
            output_price_unit VARCHAR(20) NOT NULL DEFAULT 'per_million',
            sort_order INT NOT NULL DEFAULT 0,
            synced_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            PRIMARY KEY (id, provider)
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS lore.ai_usage_events (
            id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            user_id BIGINT NOT NULL REFERENCES lore.users(id) ON DELETE CASCADE,
            kind VARCHAR(40) NOT NULL DEFAULT 'llm',
            provider VARCHAR(50) NOT NULL,
            model VARCHAR(200) NOT NULL,
            feature VARCHAR(120) NOT NULL,
            input_tokens INT NOT NULL DEFAULT 0,
            output_tokens INT NOT NULL DEFAULT 0,
            cost_usd DOUBLE PRECISION NOT NULL DEFAULT 0,
            metadata_json TEXT,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """);
    await db.Database.ExecuteSqlRawAsync("""
        CREATE INDEX IF NOT EXISTS ai_usage_events_user_created_idx
        ON lore.ai_usage_events (user_id, created_at DESC);
        """);
    await PlatformSettingsService.SeedFromEnvironmentAsync(db, cancellationToken: default);
}

using (var hangfireScope = app.Services.CreateScope())
{
    var recurringJobs = hangfireScope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var backgroundJobs = hangfireScope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    var pricingSyncCron = HangfireOptions.ReadPricingSyncCron();

    recurringJobs.AddOrUpdate<TokenMixPricingSyncJob>(
        TokenMixPricingSyncJob.JobId,
        job => job.SyncAsync(CancellationToken.None),
        pricingSyncCron);
    backgroundJobs.Enqueue<TokenMixPricingSyncJob>(job => job.SyncAsync(CancellationToken.None));

    app.Logger.LogInformation(
        "TokenMix pricing sync scheduled (cron: {Cron}); enqueued startup run.",
        pricingSyncCron);
}

app.UseHttpsRedirection();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new OperatorHangfireAuthorizationFilter()],
    DashboardTitle = "Loremetry Jobs",
});
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
app.MapWinningCatEndpoints();
app.MapProviderModelEndpoints();
app.MapPlatformSettingsEndpoints();
app.MapSettingsEndpoints();
app.MapStoryEndpoints();
app.MapSeriesEndpoints();
app.MapDocumentTypeEndpoints();
app.MapWritingAssistantEndpoints();
app.MapWritingDraftEndpoints();
app.MapAuthEndpoints();

app.Run();
