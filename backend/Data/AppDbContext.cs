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
            entity.Property(story => story.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(story => story.CreatedAt).HasColumnName("created_at");
            entity.Property(story => story.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(story => new { story.UserId, story.UpdatedAt })
                .IsDescending(false, true);
            entity.HasOne(story => story.User)
                .WithMany()
                .HasForeignKey(story => story.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(document => document.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
            entity.Property(document => document.MimeType).HasColumnName("mime_type").HasMaxLength(127).IsRequired();
            entity.Property(document => document.TextContent).HasColumnName("text_content");
            entity.Property(document => document.BinaryContent).HasColumnName("binary_content");
            entity.Property(document => document.SortOrder).HasColumnName("sort_order");
            entity.Property(document => document.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(document => new { document.SeriesId, document.SortOrder });
            entity.HasIndex(document => new { document.SeriesId, document.FileName }).IsUnique();
            entity.HasOne(document => document.Series)
                .WithMany(series => series.BibleDocuments)
                .HasForeignKey(document => document.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
