using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Collection> Collections => Set<Collection>();

    public DbSet<CollectionField> CollectionFields => Set<CollectionField>();

    public DbSet<CollectionEntry> CollectionEntries => Set<CollectionEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("collections");
            entity.HasKey(collection => collection.Id);
            entity.Property(collection => collection.Name).HasMaxLength(120).IsRequired();
            entity.Property(collection => collection.Slug).HasMaxLength(120).IsRequired();
            entity.HasIndex(collection => collection.Slug).IsUnique();
            entity.Property(collection => collection.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<CollectionField>(entity =>
        {
            entity.ToTable("collection_fields");
            entity.HasKey(field => field.Id);
            entity.Property(field => field.Name).HasMaxLength(120).IsRequired();
            entity.Property(field => field.FieldKey).HasMaxLength(120).IsRequired();
            entity.HasIndex(field => new { field.CollectionId, field.FieldKey }).IsUnique();
            entity.HasOne(field => field.Collection)
                .WithMany(collection => collection.Fields)
                .HasForeignKey(field => field.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CollectionEntry>(entity =>
        {
            entity.ToTable("collection_entries");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.ValuesJson)
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");
            entity.HasOne(entry => entry.Collection)
                .WithMany(collection => collection.Entries)
                .HasForeignKey(entry => entry.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
