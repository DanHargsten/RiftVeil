using System.Linq.Expressions;

using RiftVeil.Application.Dtos.Games;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Mappings;

public static class MatchProjections
{
    /// <summary>
    /// Approximate per-game wall-clock budget used to decide whether a Scheduled match
    /// is "probably live right now". 75 min covers a typical pro game (~35–40 min play
    /// + draft + breaks), so a BO3 gets ~3¾ h and a BO5 ~6¼ h.
    /// <para>
    /// Needed because <see cref="Match.MarkLive"/> is never called by the import (no
    /// auto-import yet — see docs/future-projects.md). Without this heuristic the UI
    /// would never show a LIVE badge between the scheduled-import and the
    /// finished-import.
    /// </para>
    /// </summary>
    public const int LiveWindowMinutesPerGame = 75;

    /// <summary>
    /// Projects matches for list views. <paramref name="now"/> is used to derive
    /// <see cref="MatchStatus.Live"/> on the fly when the DB still says Scheduled but
    /// the planned start has passed and the live window hasn't expired.
    /// </summary>
    public static Expression<Func<Match, MatchListItemDto>> ToListItemDto(DateTimeOffset now)
    {
        return match => new MatchListItemDto(
            match.Id,
            match.TournamentId,
            match.Tournament.Name,
            match.Tournament.Stage,
            match.Tournament.League.Name,
            match.Tournament.League.ShortName,
            match.Tournament.League.Region,
            match.Team1.Name,
            match.Team2.Name,
            match.Team1.ShortName,
            match.Team2.ShortName,
            match.Team1.LogoUrl,
            match.Team2.LogoUrl,
            match.Team1.IconLogoUrl,
            match.Team2.IconLogoUrl,
            match.StartsAtUtc,
            match.BestOf,
            match.Status == MatchStatus.Scheduled
                && match.StartsAtUtc <= now
                && match.StartsAtUtc.AddMinutes(match.BestOf * (double)LiveWindowMinutesPerGame) >= now
                ? MatchStatus.Live
                : match.Status,
            match.Team1Score,
            match.Team2Score,
            match.Round,
            match.Games
                .OrderBy(g => g.GameNumber)
                .Select(g => new GameDto(g.Id, g.GameNumber, g.WinningTeam, g.VodUrl, null, null, null, null))
                .ToList()
        );
    }


    /// <summary>
    /// Maps a materialized match to a details DTO. <paramref name="now"/> is used for
    /// the same derived-Live heuristic as <see cref="ToListItemDto"/>.
    /// </summary>
    public static MatchDetailsDto ToDetailsDto(this Match match, DateTimeOffset now)
    {
        var derivedStatus = match.Status == MatchStatus.Scheduled
            && match.StartsAtUtc <= now
            && match.StartsAtUtc.AddMinutes(match.BestOf * (double)LiveWindowMinutesPerGame) >= now
                ? MatchStatus.Live
                : match.Status;

        return new MatchDetailsDto(
            match.Id,
            match.Team1.Name,
            match.Team2.Name,
            match.Team1.ShortName,
            match.Team2.ShortName,
            match.Team1.LogoUrl,
            match.Team2.LogoUrl,
            match.Team1.IconLogoUrl,
            match.Team2.IconLogoUrl,
            match.StartsAtUtc,
            match.StartedAtUtc,
            match.FinishedAtUtc,
            match.BestOf,
            derivedStatus,
            match.Team1Score,
            match.Team2Score,
            match.Round,
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
                .Select(g =>
                {
                    var manualVod = g.Vods.FirstOrDefault(vod => vod.Source == VodSource.Manual);
                    return new GameDto(
                        g.Id,
                        g.GameNumber,
                        g.WinningTeam,
                        g.VodUrl,
                        g.Vods.Select(v => new GameVodDto(
                            v.Id,
                            v.Provider,
                            v.Source,
                            v.Locale,
                            v.Url,
                            v.OffsetSeconds,
                            v.DraftOffsetSeconds)).ToList(),
                        manualVod?.Url,
                        manualVod?.DraftOffsetSeconds,
                        manualVod?.OffsetSeconds);
                })
                .ToList()
        );
    }
}
