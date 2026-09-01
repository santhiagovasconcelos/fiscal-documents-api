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
}