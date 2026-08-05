using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();

    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    public DbSet<AppWideSetting> AppWideSettings => Set<AppWideSetting>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Story> Stories => Set<Story>();

    public DbSet<LoreStoryDocument> StoryDocuments => Set<LoreStoryDocument>();

    public DbSet<LoreSeries> Series => Set<LoreSeries>();

    public DbSet<SeriesBibleDocument> SeriesBibleDocuments => Set<SeriesBibleDocument>();

    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    public DbSet<WritingDraft> WritingDrafts => Set<WritingDraft>();

    public DbSet<ProviderModel> ProviderModels => Set<ProviderModel>();

    public DbSet<CanopyPricingPlan> CanopyPricingPlans => Set<CanopyPricingPlan>();

    public DbSet<AiUsageEvent> AiUsageEvents => Set<AiUsageEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.ToTable("user_settings", "lore");
            entity.HasKey(setting => new { setting.UserId, setting.SettingKey });
            entity.Property(setting => setting.UserId).HasColumnName("user_id");
            entity.Property(setting => setting.SettingKey).HasColumnName("key").HasMaxLength(120);
            entity.Property(setting => setting.Value).HasColumnName("value").IsRequired();
            entity.Property(setting => setting.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(setting => setting.User)
                .WithMany()
                .HasForeignKey(setting => setting.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformSetting>(entity =>
        {
            entity.ToTable("platform_settings", "lore");
            entity.HasKey(setting => setting.SettingKey);
            entity.Property(setting => setting.SettingKey).HasColumnName("key").HasMaxLength(120);
            entity.Property(setting => setting.Value).HasColumnName("value").IsRequired();
            entity.Property(setting => setting.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<AppWideSetting>(entity =>
        {
            entity.ToTable("app_settings", "lore");
            entity.HasKey(setting => setting.SettingKey);
            entity.Property(setting => setting.SettingKey).HasColumnName("key").HasMaxLength(120);
            entity.Property(setting => setting.Value).HasColumnName("value").IsRequired();
            entity.Property(setting => setting.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "lore");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(user => user.ClerkId).HasColumnName("clerk_id").HasMaxLength(255);
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
            entity.Property(user => user.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(user => user.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnName("created_at");
            entity.Property(user => user.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.ClerkId).IsUnique();
        });

        modelBuilder.Entity<Story>(entity =>
        {
            entity.ToTable("stories", "lore");
            entity.HasKey(story => story.Id);
            entity.Property(story => story.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(story => story.UserId).HasColumnName("user_id");
            entity.Property(story => story.SeriesId).HasColumnName("series_id");
            entity.Property(story => story.SeriesSortOrder).HasColumnName("series_sort_order");
            entity.Property(story => story.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(story => story.CreatedAt).HasColumnName("created_at");
            entity.Property(story => story.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(story => new { story.UserId, story.UpdatedAt })
                .IsDescending(false, true);
            entity.HasIndex(story => new { story.SeriesId, story.SeriesSortOrder });
            entity.HasOne(story => story.User)
                .WithMany()
                .HasForeignKey(story => story.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(story => story.Series)
                .WithMany(series => series.Stories)
                .HasForeignKey(story => story.SeriesId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LoreStoryDocument>(entity =>
        {
            entity.ToTable("story_documents", "lore");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(document => document.StoryId).HasColumnName("story_id");
            entity.Property(document => document.Kind).HasColumnName("kind").HasMaxLength(20).IsRequired();
            entity.Property(document => document.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            entity.Property(document => document.MimeType).HasColumnName("mime_type").HasMaxLength(127).IsRequired();
            entity.Property(document => document.TextContent).HasColumnName("text_content");
            entity.Property(document => document.BinaryContent).HasColumnName("binary_content");
            entity.Property(document => document.SortOrder).HasColumnName("sort_order");
            entity.Property(document => document.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(document => new { document.StoryId, document.SortOrder });
            entity.HasIndex(document => new { document.StoryId, document.Kind, document.FileName }).IsUnique();
            entity.HasOne(document => document.Story)
                .WithMany(story => story.Documents)
                .HasForeignKey(document => document.StoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoreSeries>(entity =>
        {
            entity.ToTable("series", "lore");
            entity.HasKey(series => series.Id);
            entity.Property(series => series.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(series => series.UserId).HasColumnName("user_id");
            entity.Property(series => series.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(series => series.CreatedAt).HasColumnName("created_at");
            entity.Property(series => series.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(series => new { series.UserId, series.UpdatedAt })
                .IsDescending(false, true);
            entity.HasOne(series => series.User)
                .WithMany()
                .HasForeignKey(series => series.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SeriesBibleDocument>(entity =>
        {
            entity.ToTable("series_bible_documents", "lore");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(document => document.SeriesId).HasColumnName("series_id");
            entity.Property(document => document.Category).HasColumnName("category").HasMaxLength(20).IsRequired();
            entity.Property(document => document.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            entity.Property(document => document.MimeType).HasColumnName("mime_type").HasMaxLength(127).IsRequired();
            entity.Property(document => document.TextContent).HasColumnName("text_content");
            entity.Property(document => document.BinaryContent).HasColumnName("binary_content");
            entity.Property(document => document.SortOrder).HasColumnName("sort_order");
            entity.Property(document => document.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(document => new { document.SeriesId, document.SortOrder });
            entity.HasIndex(document => new { document.SeriesId, document.Category, document.FileName }).IsUnique();
            entity.HasOne(document => document.Series)
                .WithMany(series => series.BibleDocuments)
                .HasForeignKey(document => document.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.ToTable("document_types", "lore");
            entity.HasKey(type => type.Code);
            entity.Property(type => type.Code).HasColumnName("code").HasMaxLength(20);
            entity.Property(type => type.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            entity.Property(type => type.SortOrder).HasColumnName("sort_order");
            entity.Property(type => type.AppliesToStory).HasColumnName("applies_to_story");
            entity.Property(type => type.AppliesToSeries).HasColumnName("applies_to_series");
            entity.Property(type => type.Active).HasColumnName("active");
        });

        modelBuilder.Entity<WritingDraft>(entity =>
        {
            entity.ToTable("writing_drafts", "lore");
            entity.HasKey(draft => draft.Id);
            entity.Property(draft => draft.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(draft => draft.UserId).HasColumnName("user_id");
            entity.Property(draft => draft.DraftKey).HasColumnName("draft_key").HasMaxLength(500).IsRequired();
            entity.Property(draft => draft.Mode).HasColumnName("mode").HasMaxLength(20).IsRequired();
            entity.Property(draft => draft.Source).HasColumnName("source").HasMaxLength(20);
            entity.Property(draft => draft.ParentId).HasColumnName("parent_id");
            entity.Property(draft => draft.DocumentId).HasColumnName("document_id");
            entity.Property(draft => draft.Category).HasColumnName("category").HasMaxLength(20).IsRequired();
            entity.Property(draft => draft.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(draft => draft.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            entity.Property(draft => draft.TextContent).HasColumnName("text_content").IsRequired();
            entity.Property(draft => draft.DestinationKey).HasColumnName("destination_key").HasMaxLength(120).IsRequired();
            entity.Property(draft => draft.MimeType).HasColumnName("mime_type").HasMaxLength(127).IsRequired();
            entity.Property(draft => draft.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(draft => new { draft.UserId, draft.DraftKey }).IsUnique();
            entity.HasOne(draft => draft.User)
                .WithMany()
                .HasForeignKey(draft => draft.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProviderModel>(entity =>
        {
            entity.ToTable("provider_models", "lore");
            entity.HasKey(model => new { model.Id, model.Provider });
            entity.Property(model => model.Id).HasColumnName("id").HasMaxLength(200);
            entity.Property(model => model.Provider).HasColumnName("provider").HasMaxLength(50);
            entity.Property(model => model.OwnedBy).HasColumnName("owned_by").HasMaxLength(100).IsRequired();
            entity.Property(model => model.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            entity.Property(model => model.ModelType).HasColumnName("model_type").HasMaxLength(50).IsRequired();
            entity.Property(model => model.InputPrice).HasColumnName("input_price");
            entity.Property(model => model.OutputPrice).HasColumnName("output_price");
            entity.Property(model => model.InputPriceUnit).HasColumnName("input_price_unit").HasMaxLength(20).IsRequired();
            entity.Property(model => model.OutputPriceUnit).HasColumnName("output_price_unit").HasMaxLength(20).IsRequired();
            entity.Property(model => model.SortOrder).HasColumnName("sort_order");
            entity.Property(model => model.SyncedAt).HasColumnName("synced_at");
            entity.HasIndex(model => new { model.Provider, model.SortOrder });
        });

        modelBuilder.Entity<CanopyPricingPlan>(entity =>
        {
            entity.ToTable("canopy_pricing_plans", "lore");
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Id).HasColumnName("id").HasMaxLength(50);
            entity.Property(plan => plan.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
            entity.Property(plan => plan.MonthlyFeeUsd).HasColumnName("monthly_fee_usd");
            entity.Property(plan => plan.MonthlyRequestAllowance).HasColumnName("monthly_request_allowance");
            entity.Property(plan => plan.OveragePricePerRequest).HasColumnName("overage_price_per_request");
            entity.Property(plan => plan.SortOrder).HasColumnName("sort_order");
            entity.Property(plan => plan.SyncedAt).HasColumnName("synced_at");
        });

        modelBuilder.Entity<AiUsageEvent>(entity =>
        {
            entity.ToTable("ai_usage_events", "lore");
            entity.HasKey(usage => usage.Id);
            entity.Property(usage => usage.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(usage => usage.UserId).HasColumnName("user_id");
            entity.Property(usage => usage.Kind).HasColumnName("kind").HasMaxLength(40).IsRequired();
            entity.Property(usage => usage.Provider).HasColumnName("provider").HasMaxLength(50).IsRequired();
            entity.Property(usage => usage.Model).HasColumnName("model").HasMaxLength(200).IsRequired();
            entity.Property(usage => usage.Feature).HasColumnName("feature").HasMaxLength(120).IsRequired();
            entity.Property(usage => usage.InputTokens).HasColumnName("input_tokens");
            entity.Property(usage => usage.OutputTokens).HasColumnName("output_tokens");
            entity.Property(usage => usage.CostUsd).HasColumnName("cost_usd");
            entity.Property(usage => usage.MetadataJson).HasColumnName("metadata_json");
            entity.Property(usage => usage.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(usage => new { usage.UserId, usage.CreatedAt }).IsDescending(false, true);
            entity.HasOne(usage => usage.User)
                .WithMany()
                .HasForeignKey(usage => usage.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
