using FiscalDocuments.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FiscalDocuments.Api.Data;

public class FiscalDocumentsDbContext : DbContext
{
    public FiscalDocumentsDbContext(
        DbContextOptions<FiscalDocumentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<FiscalDocument> FiscalDocuments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FiscalDocument>()
            .HasIndex(x => x.AccessKey)
            .IsUnique();

        modelBuilder.Entity<FiscalDocument>()
            .Property(x => x.AccessKey)
            .HasMaxLength(44);
    }
}

