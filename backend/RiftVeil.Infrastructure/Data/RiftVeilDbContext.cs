using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Common;
using RiftVeil.Domain.Entities;

namespace RiftVeil.Infrastructure.Data;

/// <summary>
/// Central EF Core context that enforces model rules and timestamps.
/// </summary>
public class RiftVeilDbContext(DbContextOptions<RiftVeilDbContext> options) : DbContext(options)
{
    public DbSet<DbSmokeTest> DbSmokeTest => Set<DbSmokeTest>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<League>(entity =>
        {
            entity.Property(league => league.Name).IsRequired().HasMaxLength(100);
            entity.Property(league => league.ShortName).IsRequired().HasMaxLength(20);
            entity.Property(league => league.Region).HasMaxLength(100);
            entity.Property(league => league.LogoUrl).HasMaxLength(2048);
            entity.Property(league => league.ExternalId).HasMaxLength(100);

            entity.HasIndex(league => league.ShortName).IsUnique();

            entity.HasMany(league => league.Tournaments)
                .WithOne(tournament => tournament.League)
                .HasForeignKey(tournament => tournament.LeagueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.Property(tournament => tournament.Name).IsRequired().HasMaxLength(100);
            entity.Property(tournament => tournament.StartsAtUtc).IsRequired();
            entity.Property(tournament => tournament.ExternalId).HasMaxLength(100);
            entity.Property(tournament => tournament.LiquipediaSlug).HasMaxLength(200);

            entity.HasIndex(tournament => tournament.StartsAtUtc);

            entity.HasMany(tournament => tournament.Matches)
                .WithOne(match => match.Tournament)
                .HasForeignKey(match => match.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.Property(match => match.Team1Name).IsRequired().HasMaxLength(100);
            entity.Property(match => match.Team2Name).IsRequired().HasMaxLength(100);
            entity.Property(match => match.StartsAtUtc).IsRequired();
            entity.Property(match => match.VodUrl).HasMaxLength(2048);
            entity.Property(match => match.ExternalId).HasMaxLength(100);

            entity.HasIndex(match => match.StartsAtUtc);
            entity.HasIndex(match => new { match.TournamentId, match.Status });

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Match_BestOf_AllowedValues",
                    "[BestOf] IN (1, 2, 3, 5)"
                );
            });
        });
    }

    /// <summary>
    /// Ensures timestamps are set before persisting changes.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }


    /// <summary>
    /// Ensures timestamps are set before persisting changes.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }


    /// <summary>
    /// Applies create/update timestamps consistently across entities.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
