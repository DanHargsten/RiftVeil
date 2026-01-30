using System.Linq.Expressions;
using RiftVeil.Domain.Entities;
using RiftVeil.Application.DTOs.Matches;
using RiftVeil.Application.DTOs.Leagues;
using RiftVeil.Application.DTOs.Tournaments;

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
            tournament.Name,
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
                m.Team1Name,
                m.Team2Name,
                m.StartsAtUtc,
                m.BestOf,
                m.Status
            ))]
        );
    }
}
