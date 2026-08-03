using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();

    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    public DbSet<User> Users => Set<User>();

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
    }
}
