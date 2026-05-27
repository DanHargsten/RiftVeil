using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Common;
using RiftVeil.Domain.Entities;

namespace RiftVeil.Infrastructure.Data;

/// <summary>
/// Central EF Core context that enforces model rules and timestamps.
/// </summary>
public class RiftVeilDbContext(DbContextOptions<RiftVeilDbContext> options) : DbContext(options)
{
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<GameVod> GameVods => Set<GameVod>();
    public DbSet<GamePlayerStats> GamePlayerStats => Set<GamePlayerStats>();
    public DbSet<GameTeamStats> GameTeamStats => Set<GameTeamStats>();
    public DbSet<GameDraftEntry> GameDraftEntries => Set<GameDraftEntry>();

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
            entity.Property(tournament => tournament.Stage).HasMaxLength(100);
            entity.Property(tournament => tournament.StartsAtUtc).IsRequired();
            entity.Property(tournament => tournament.ExternalId).HasMaxLength(100);
            entity.Property(tournament => tournament.LiquipediaSlug).HasMaxLength(200);

            entity.HasIndex(tournament => tournament.StartsAtUtc);

            entity.HasMany(tournament => tournament.Matches)
                .WithOne(match => match.Tournament)
                .HasForeignKey(match => match.TournamentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(team => team.Name).IsRequired().HasMaxLength(100);
            entity.Property(team => team.ShortName).IsRequired().HasMaxLength(20);
            entity.Property(team => team.Region).HasMaxLength(100);
            entity.Property(team => team.LogoUrl).HasMaxLength(2048);
            entity.Property(team => team.IconLogoUrl).HasMaxLength(2048);
            entity.Property(team => team.ExternalId).HasMaxLength(100);

            entity.HasIndex(team => team.ShortName).IsUnique();
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.Property(match => match.StartsAtUtc).IsRequired();
            entity.Property(match => match.Round).HasMaxLength(100);
            entity.Property(match => match.VodUrl).HasMaxLength(2048);
            entity.Property(match => match.ExternalId).HasMaxLength(100);

            entity.HasIndex(match => match.StartsAtUtc);
            entity.HasIndex(match => new { match.TournamentId, match.Status });

            entity.HasOne(match => match.Team1)
                .WithMany(team => team.MatchesAsTeam1)
                .HasForeignKey(match => match.Team1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(match => match.Team2)
                .WithMany(team => team.MatchesAsTeam2)
                .HasForeignKey(match => match.Team2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(match => match.Games)
                .WithOne(game => game.Match)
                .HasForeignKey(game => game.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Match_BestOf_AllowedValues",
                    "[BestOf] IN (1, 2, 3, 5)"
                );
            });
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.Property(game => game.GameNumber).IsRequired();
            entity.Property(game => game.Team1Side).HasMaxLength(10);
            entity.Property(game => game.Team2Side).HasMaxLength(10);
            entity.Property(game => game.VodUrl).HasMaxLength(2048);
            entity.Property(game => game.ExternalId).HasMaxLength(100);

            entity.HasIndex(game => new { game.MatchId, game.GameNumber }).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Game_WinningTeam_AllowedValues",
                    "[WinningTeam] IS NULL OR [WinningTeam] IN (1, 2)"
                );
                table.HasCheckConstraint(
                    "CK_Game_Team1Side_AllowedValues",
                    "[Team1Side] IS NULL OR [Team1Side] IN ('Blue', 'Red')"
                );
                table.HasCheckConstraint(
                    "CK_Game_Team2Side_AllowedValues",
                    "[Team2Side] IS NULL OR [Team2Side] IN ('Blue', 'Red')"
                );
            });
        });

        modelBuilder.Entity<GameVod>(entity =>
        {
            entity.Property(v => v.Provider).IsRequired();
            entity.Property(v => v.Locale).HasMaxLength(10);
            entity.Property(v => v.Source).IsRequired().HasDefaultValue(Domain.Enums.VodSource.Imported);
            entity.Property(v => v.Url).IsRequired().HasMaxLength(2048);
            entity.Property(v => v.Parameter).HasMaxLength(200);
            entity.Property(v => v.Priority).HasDefaultValue(0);

            entity.HasIndex(v => new { v.GameId, v.Provider, v.Locale, v.Source }).IsUnique();

            entity.HasOne(v => v.Game)
                .WithMany(g => g.Vods)
                .HasForeignKey(v => v.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<GamePlayerStats>(entity =>
        {
            entity.Property(p => p.PlayerName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.IngameRole).IsRequired().HasMaxLength(20);
            entity.Property(p => p.Champion).IsRequired().HasMaxLength(100);
            entity.Property(p => p.ItemIds).HasMaxLength(200);
            entity.Property(p => p.TrinketId).HasMaxLength(20);
            entity.Property(p => p.SummonerSpell1Id).HasMaxLength(50);
            entity.Property(p => p.SummonerSpell2Id).HasMaxLength(50);

            entity.HasIndex(p => new { p.GameId, p.PlayerName }).IsUnique();

            entity.HasOne(p => p.Game)
                .WithMany(g => g.PlayerStats)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(table =>
                table.HasCheckConstraint("CK_GamePlayerStats_TeamNumber", "[TeamNumber] IN (1, 2)")
            );
        });

        modelBuilder.Entity<GameTeamStats>(entity =>
        {
            entity.HasIndex(t => new { t.GameId, t.TeamNumber }).IsUnique();

            entity.HasOne(t => t.Game)
                .WithMany(g => g.TeamStats)
                .HasForeignKey(t => t.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(table =>
                table.HasCheckConstraint("CK_GameTeamStats_TeamNumber", "[TeamNumber] IN (1, 2)")
            );
        });

        modelBuilder.Entity<GameDraftEntry>(entity =>
        {
            entity.Property(d => d.Phase).IsRequired().HasMaxLength(10);
            entity.Property(d => d.Champion).IsRequired().HasMaxLength(100);

            entity.HasIndex(d => new { d.GameId, d.SequenceNumber }).IsUnique();

            entity.HasOne(d => d.Game)
                .WithMany(g => g.DraftEntries)
                .HasForeignKey(d => d.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_GameDraftEntries_TeamNumber", "[TeamNumber] IN (1, 2)");
                table.HasCheckConstraint("CK_GameDraftEntries_Phase", "[Phase] IN ('Ban', 'Pick')");
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
