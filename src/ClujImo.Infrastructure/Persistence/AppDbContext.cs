using ClujImo.Domain.Addresses;
using ClujImo.Domain.Geo;
using ClujImo.Domain.Investments;
using ClujImo.Domain.Permits;
using ClujImo.Domain.Planning;
using ClujImo.Domain.Statistics;
using Microsoft.EntityFrameworkCore;

namespace ClujImo.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Permit> Permits => Set<Permit>();

    public DbSet<Street> Streets => Set<Street>();

    public DbSet<IngestionRun> IngestionRuns => Set<IngestionRun>();

    public DbSet<ProcessedResource> ProcessedResources => Set<ProcessedResource>();

    public DbSet<OsmAddress> OsmAddresses => Set<OsmAddress>();

    public DbSet<NeighborhoodArea> Neighborhoods => Set<NeighborhoodArea>();

    public DbSet<ConstructionSite> ConstructionSites => Set<ConstructionSite>();

    public DbSet<MetroStat> MetroStats => Set<MetroStat>();

    public DbSet<UrbanPlan> UrbanPlans => Set<UrbanPlan>();

    public DbSet<CouncilDecision> CouncilDecisions => Set<CouncilDecision>();

    public DbSet<PublicPayment> PublicPayments => Set<PublicPayment>();

    public DbSet<PublicContract> PublicContracts => Set<PublicContract>();

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
            permit.HasIndex(p => new { p.Latitude, p.Longitude });
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

        modelBuilder.Entity<IngestionRun>(run =>
        {
            run.Property(r => r.Source).HasMaxLength(32);
            run.HasIndex(r => new { r.Source, r.Id });
        });

        modelBuilder.Entity<ProcessedResource>(resource =>
        {
            resource.HasKey(r => r.Url);
            resource.Property(r => r.Source).HasMaxLength(32);
        });

        modelBuilder.Entity<OsmAddress>(address =>
        {
            address.Property(a => a.StreetKey).HasMaxLength(128);
            address.Property(a => a.HouseNumber).HasMaxLength(32);
            address.HasIndex(a => a.StreetKey);
        });

        modelBuilder.Entity<NeighborhoodArea>(area =>
        {
            area.HasKey(a => a.Name);
            area.Property(a => a.Name).HasMaxLength(128);
            area.Ignore(a => a.Center);
            area.Ignore(a => a.Rings);
            area.Ignore(a => a.HasPolygon);
        });

        modelBuilder.Entity<ConstructionSite>(site =>
        {
            site.Property(s => s.Id).HasMaxLength(32);
            site.Property(s => s.Name).HasMaxLength(256);
            site.Property(s => s.Building).HasMaxLength(64);
            site.Property(s => s.Neighborhood).HasMaxLength(128);
        });

        modelBuilder.Entity<MetroStat>(stat =>
        {
            stat.HasIndex(s => new { s.Indicator, s.Siruta, s.Year }).IsUnique();
            stat.Property(s => s.Siruta).HasMaxLength(16);
            stat.Property(s => s.Locality).HasMaxLength(128);
        });

        modelBuilder.Entity<UrbanPlan>(plan =>
        {
            plan.HasIndex(p => new { p.Stage, p.Reference }).IsUnique();
            plan.HasIndex(p => p.Date);
            plan.Property(p => p.Reference).HasMaxLength(32);
            plan.Property(p => p.Initiator).HasMaxLength(256);
            plan.Property(p => p.SourceUrl).HasMaxLength(512);
            plan.Property(p => p.StreetKey).HasMaxLength(128);
            plan.Property(p => p.StreetDisplay).HasMaxLength(256);
            plan.Property(p => p.HouseNumber).HasMaxLength(64);
            plan.Property(p => p.Neighborhood).HasMaxLength(128);
        });

        modelBuilder.Entity<CouncilDecision>(decision =>
        {
            decision.HasIndex(d => new { d.Year, d.Number }).IsUnique();
            decision.HasIndex(d => d.Topic);
            decision.Property(d => d.Url).HasMaxLength(512);
        });

        modelBuilder.Entity<PublicPayment>(payment =>
        {
            payment.HasIndex(p => p.RowKey).IsUnique();
            payment.HasIndex(p => p.Date);
            payment.Property(p => p.RowKey).HasMaxLength(64);
            payment.Property(p => p.Beneficiary).HasMaxLength(256);
            payment.Property(p => p.Amount).HasConversion<double>();
        });

        modelBuilder.Entity<PublicContract>(contract =>
        {
            contract.HasIndex(c => new { c.ContractNumber, c.AwardNoticeNumber, c.Lot }).IsUnique();
            contract.HasIndex(c => c.ContractDate);
            contract.Property(c => c.AuthorityCui).HasMaxLength(16);
            contract.Property(c => c.ContractNumber).HasMaxLength(64);
            contract.Property(c => c.AwardNoticeNumber).HasMaxLength(64);
            contract.Property(c => c.Lot).HasMaxLength(16);
            contract.Property(c => c.CpvCode).HasMaxLength(16);
            contract.Property(c => c.WinnerCui).HasMaxLength(32);
            contract.Property(c => c.ValueRon).HasConversion<double?>();
            contract.Ignore(c => c.IsWorks);
        });

        modelBuilder.Entity<MetroStat>().Property(s => s.Value).HasConversion<double>();
    }
}
