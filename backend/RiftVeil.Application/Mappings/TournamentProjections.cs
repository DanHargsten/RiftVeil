using System.Linq.Expressions;
using RiftVeil.Domain.Entities;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Application.Dtos.Games;

namespace RiftVeil.Application.Mappings;

public static class TournamentProjections
{
    /// <summary>
    /// Projects tournaments for list views.
    /// </summary>
    public static Expression<Func<Tournament, TournamentListItemDto>> ToListItemDto()
    {
        return tournament => new TournamentListItemDto(
            tournament.Id,
            tournament.LeagueId,
            tournament.League.Name,
            tournament.League.ShortName,
            tournament.Name,
            tournament.Stage,
            tournament.StartsAtUtc,
            tournament.EndsAtUtc,
            tournament.Status
        );
    }


    /// <summary>
    /// Maps a materialized tournament to a details DTO.
    /// </summary>
    public static TournamentDetailsDto ToDetailsDto(this Tournament tournament)
    {
        return new TournamentDetailsDto(
            tournament.Id,
            tournament.Name,
            tournament.StartsAtUtc,
            tournament.EndsAtUtc,
            tournament.Status,
            tournament.LiquipediaSlug,
            new LeagueListItemDto(
                tournament.League.Id,
                tournament.League.Name,
                tournament.League.ShortName,
                tournament.League.Region,
                tournament.League.LogoUrl
            ),
            [.. tournament.Matches.Select(m => new MatchListItemDto(
                m.Id,
                m.TournamentId,
                tournament.Name,
                tournament.Stage,
                tournament.League.Name,
                tournament.League.ShortName,
                m.Team1.Name,
                m.Team2.Name,
                m.Team1.ShortName,
                m.Team2.ShortName,
                m.StartsAtUtc,
                m.BestOf,
                m.Status,
                m.Team1Score,
                m.Team2Score,
                m.Games
                    .OrderBy(g => g.GameNumber)
                    .Select(g => new GameDto(g.Id, g.GameNumber, g.WinningTeam, g.VodUrl))
                    .ToList()
            ))]
        );
    }
}
