using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Games;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Domain.Common;
using RiftVeil.Domain.Entities;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// HTTP API for game-level details (stats, draft) and admin VOD overrides.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GamesController(
    IGameReadService gameReadService,
    RiftVeilDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Get full details for a specific game.
    /// </summary>
    /// <param name="gameId">Primary key of the game in the local database.</param>
    [HttpGet("{gameId}/details")]
    [ProducesResponseType(typeof(GameDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDetailsDto>> GetDetailsAsync(int gameId)
    {
        var details = await gameReadService.GetDetailsByIdAsync(gameId);

        if (details == null)
            return NotFound();

        return Ok(details);
    }

    /// <summary>
    /// Sets or clears a manual VOD URL for a game (e.g. third-party YouTube highlights with start offsets).
    /// </summary>
    [HttpPatch("{gameId:int}/vod")]
    [ProducesResponseType(typeof(GameVodUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameVodUpdateResultDto>> UpdateVodAsync(
        int gameId,
        [FromBody] UpdateGameVodRequest request)
    {
        var game = await dbContext.Games
            .Include(storedGame => storedGame.Vods)
            .FirstOrDefaultAsync(storedGame => storedGame.Id == gameId);

        if (game == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            game.RemoveManualVods();
            game.SetVodUrl(VodSelectors.GetBestVodUrl(game.Vods));
            await dbContext.SaveChangesAsync();
            return Ok(ToVodUpdateResult(game));
        }

        if (!GameVodUrls.TryParseProvider(request.Url, out var provider))
            return BadRequest("URL must be a YouTube or Twitch link.");

        var gameStartOffset = request.GameStartOffsetSeconds
            ?? (request.OffsetSeconds > 0 ? request.OffsetSeconds : null);

        if (request.DraftOffsetSeconds is < 0 || gameStartOffset is < 0)
            return BadRequest("Offsets cannot be negative.");

        var parameter = GameVodUrls.TryExtractParameter(request.Url, provider);
        game.ApplyManualVod(
            provider,
            request.Url.Trim(),
            parameter,
            request.DraftOffsetSeconds,
            gameStartOffset);
        await dbContext.SaveChangesAsync();

        return Ok(ToVodUpdateResult(game));
    }

    private static GameVodUpdateResultDto ToVodUpdateResult(Game game)
    {
        var manualVod = game.Vods.FirstOrDefault(vod => vod.Locale == "manual");
        return new GameVodUpdateResultDto(
            game.Id,
            game.GameNumber,
            game.VodUrl,
            manualVod?.Url,
            manualVod?.DraftOffsetSeconds,
            manualVod?.OffsetSeconds);
    }
}
