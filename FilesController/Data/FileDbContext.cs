using FileStorageApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStorageApi.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options)
    {
    }

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(x => x.StoredFileName).IsRequired().HasMaxLength(255);
            entity.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.RelativePath).IsRequired().HasMaxLength(500);
            entity.Property(x => x.Category).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Description).HasMaxLength(500);
        });
    }
}
