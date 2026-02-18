using System.Linq.Expressions;
using RiftVeil.Domain.Entities;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Application.Dtos.Games;

namespace RiftVeil.Application.Mappings;

public static class MatchProjections
{
    /// <summary>
    /// Projects matches for list views.
    /// </summary>
    public static Expression<Func<Match, MatchListItemDto>> ToListItemDto()
    {
        return match => new MatchListItemDto(
            match.Id,
            match.TournamentId,
            match.Tournament.Name,
            match.Tournament.Stage,
            match.Tournament.League.Name,
            match.Tournament.League.ShortName,
            match.Team1.Name,
            match.Team2.Name,
            match.Team1.ShortName,
            match.Team2.ShortName,
            match.StartsAtUtc,
            match.BestOf,
            match.Status,
            match.Team1Score,
            match.Team2Score,
            match.Games
                .OrderBy(g => g.GameNumber)
                .Select(g => new GameDto(g. Id, g.GameNumber, g.WinningTeam, g.VodUrl))
                .ToList()
        );
    }


    /// <summary>
    /// Maps a materialized match to a details DTO.
    /// </summary>
    public static MatchDetailsDto ToDetailsDto(this Match match)
    {
        return new MatchDetailsDto(
            match.Id,
            match.Team1.Name,
            match.Team2.Name,
            match.Team1.ShortName,
            match.Team2.ShortName,
            match.StartsAtUtc,
            match.StartedAtUtc,
            match.FinishedAtUtc,
            match.BestOf,
            match.Status,
            match.Team1Score,
            match.Team2Score,
            match.VodUrl,
            new TournamentListItemDto(
                match.Tournament.Id,
                match.Tournament.LeagueId,
                match.Tournament.League.Name,
                match.Tournament.League.ShortName,
                match.Tournament.Name,
                match.Tournament.Stage,
                match.Tournament.StartsAtUtc,
                match.Tournament.EndsAtUtc,
                match.Tournament.Status
            ),
            match.Games
                .OrderBy(g => g.GameNumber)
                .Select(g => new GameDto(g.Id, g.GameNumber, g.WinningTeam, g.VodUrl))
                .ToList()
        );
    }
}
