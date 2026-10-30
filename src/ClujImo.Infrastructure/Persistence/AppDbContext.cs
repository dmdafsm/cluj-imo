using ClujImo.Domain.Addresses;
using ClujImo.Domain.Permits;
using Microsoft.EntityFrameworkCore;

namespace ClujImo.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Permit> Permits => Set<Permit>();

    public DbSet<Street> Streets => Set<Street>();

    public DbSet<IngestionRun> IngestionRuns => Set<IngestionRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permit>(permit =>
        {
            permit.HasIndex(p => new { p.Kind, p.Year, p.Number }).IsUnique();
            permit.HasIndex(p => p.IssuedOn);
            permit.HasIndex(p => p.Category);
            permit.HasIndex(p => p.StreetKey);
            permit.HasIndex(p => p.Neighborhood);
            permit.Property(p => p.Number).HasMaxLength(32);
            permit.Property(p => p.RequestNumber).HasMaxLength(32);
            permit.Property(p => p.ReferencedPermitNumber).HasMaxLength(32);
            permit.Property(p => p.StreetKey).HasMaxLength(128);
            permit.Property(p => p.StreetDisplay).HasMaxLength(256);
            permit.Property(p => p.HouseNumber).HasMaxLength(64);
            permit.Property(p => p.Neighborhood).HasMaxLength(128);
            permit.Property(p => p.HeightRegime).HasMaxLength(64);
            permit.Ignore(p => p.ProcessingDays);
        });

        modelBuilder.Entity<Street>(street =>
        {
            street.HasKey(s => s.Key);
            street.Property(s => s.Key).HasMaxLength(128);
            street.Property(s => s.Name).HasMaxLength(256);
            street.Property(s => s.FormerName).HasMaxLength(256);
            street.Property(s => s.FiscalZone).HasMaxLength(4);
            street.Property(s => s.Area).HasMaxLength(256);
            street.Property(s => s.Neighborhood).HasMaxLength(128);
            street.HasIndex(s => s.GeocodeStatus);
        });
    }
}
