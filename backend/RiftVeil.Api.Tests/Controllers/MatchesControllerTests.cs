using System.Net;
using System.Net.Http.Json;

using RiftVeil.Api.Tests.Infrastructure;
using RiftVeil.Application.Dtos.Matches;

namespace RiftVeil.Api.Tests.Controllers;

[Collection("Database collection")]
public class MatchesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MatchesControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllMatches_ReturnsOk_WithSeededMatches()
    {
        // Act
        var response = await _client.GetAsync("/api/matches");

        // Assert
        response.EnsureSuccessStatusCode();

        var matches = await response.Content.ReadFromJsonAsync<List<MatchListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(matches);
        Assert.Equal(4, matches.Count); // 3 scheduled + 1 finished
    }

    [Fact]
    public async Task GetAllMatches_FilterByTournamentId_ReturnsOnlyLecMatches()
    {
        // Act - Filter by LEC Spring tournament (tournamentId=1)
        var response = await _client.GetAsync("/api/matches?tournamentId=1");

        // Assert
        response.EnsureSuccessStatusCode();

        var matches = await response.Content.ReadFromJsonAsync<List<MatchListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(matches);
        Assert.Equal(3, matches.Count); // 3 LEC matches
        Assert.All(matches, m => Assert.Equal(1, m.TournamentId));
    }

    [Fact]
    public async Task GetAllMatches_FilterByStatus_ReturnsOnlyScheduledMatches()
    {
        // Act - Filter by Scheduled status
        var response = await _client.GetAsync("/api/matches?status=Scheduled");

        // Assert
        response.EnsureSuccessStatusCode();

        var matches = await response.Content.ReadFromJsonAsync<List<MatchListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(matches);
        Assert.Equal(3, matches.Count);
        Assert.All(matches, m => Assert.Equal("Scheduled", m.Status.ToString()));
    }

    [Fact]
    public async Task GetUpcomingMatches_ReturnsOnlyFutureScheduledMatches()
    {
        // Act - Get upcoming matches for next 7 days
        var response = await _client.GetAsync("/api/matches/upcoming?days=7");

        // Assert
        response.EnsureSuccessStatusCode();

        var matches = await response.Content.ReadFromJsonAsync<List<MatchListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(matches);

        // Should return today's match, tomorrow's match, and next week's match
        Assert.Equal(3, matches.Count);

        // All should be in the future
        var now = DateTimeOffset.UtcNow;
        Assert.All(matches, m =>
        {
            Assert.True(m.StartsAtUtc >= now, $"Match should be in the future: {m.Team1Name} vs {m.Team2Name}");
        });

        // Should be ordered by start time
        for (int i = 0; i < matches.Count - 1; i++)
        {
            Assert.True(matches[i].StartsAtUtc <= matches[i + 1].StartsAtUtc, "Matches should be ordered by start time");
        }
    }

    [Fact]
    public async Task GetUpcomingMatches_WithShortTimeWindow_ReturnsOnlyNearMatches()
    {
        // Act - Get upcoming matches for next 1 day
        var response = await _client.GetAsync("/api/matches/upcoming?days=1");

        // Assert
        response.EnsureSuccessStatusCode();

        var matches = await response.Content.ReadFromJsonAsync<List<MatchListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(matches);

        // Should only return today's and tomorrow's matches (2 matches)
        Assert.Equal(2, matches.Count);
    }

    [Theory]
    [InlineData("/api/matches?tournamentId=0")]
    [InlineData("/api/matches?from=2026-07-02T00:00:00Z&to=2026-07-01T00:00:00Z")]
    [InlineData("/api/matches/upcoming?days=0")]
    [InlineData("/api/matches/upcoming?days=91")]
    [InlineData("/api/matches/recent?count=0")]
    [InlineData("/api/matches/recent?count=101")]
    public async Task GetMatches_WithInvalidQueryRange_ReturnsBadRequest(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMatchById_ReturnsOk_WithMatchDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/matches/1");

        // Assert
        response.EnsureSuccessStatusCode();

        var match = await response.Content.ReadFromJsonAsync<MatchDetailsDto>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(match);
        Assert.Equal(1, match.Id);
        Assert.NotNull(match.Tournament);
        Assert.Equal("Fnatic", match.Team1Name);
        Assert.Equal("G2 Esports", match.Team2Name);
    }

    [Fact]
    public async Task GetMatchById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/matches/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
