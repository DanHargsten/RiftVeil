using System.Linq.Expressions;
using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Domain.Entities;

namespace RiftVeil.Application.Mappings;

public static class LeagueProjections
{
    /// <summary>
    /// Projects leagues for list views.
    /// </summary>
    public static Expression<Func<League, LeagueListItemDto>> ToListItemDto()
    {
        return league => new LeagueListItemDto(
            league.Id,
            league.Name,
            league.ShortName,
            league.Region,
            league.LogoUrl
        );
    }


    /// <summary>
    /// Maps a materialized league to a details DTO.
    /// </summary>
    public static LeagueDetailsDto ToDetailsDto(this League league)
    {
        return new LeagueDetailsDto(
            league.Id,
            league.Name,
            league.ShortName,
            league.Region,
            league.LogoUrl,
            [.. league.Tournaments.Select(t => new TournamentListItemDto(
                t.Id,
                t.LeagueId,
                t.Name,
                t.StartsAtUtc,
                t.EndsAtUtc,
                t.Status
            ))]
        );
    }
}
