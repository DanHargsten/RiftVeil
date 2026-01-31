using System.Net;
using System.Net.Http.Json;
using RiftVeil.Api.Tests.Infrastructure;
using RiftVeil.Application.Dtos.Tournaments;

namespace RiftVeil.Api.Tests.Controllers;

[Collection("Database collection")]
public class TournamentsControllersTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TournamentsControllersTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllTournaments_ReturnsOk_WithSeededTournaments()
    {
        // Act
        var response = await _client.GetAsync("/api/tournaments");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var tournaments = await response.Content.ReadFromJsonAsync<List<TournamentListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(tournaments);
        Assert.Equal(3, tournaments.Count); // LEC Spring, LCS Spring, Worlds 2025
    }

    [Fact]
    public async Task GetAllTournaments_FilterByLeagueId_ReturnsOnlyLecTournaments()
    {
        // Act - Filter by LEC (leagueId=1)
        var response = await _client.GetAsync("/api/tournaments?leagueId=1");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var tournaments = await response.Content.ReadFromJsonAsync<List<TournamentListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(tournaments);
        Assert.Single(tournaments); // Only LEC Spring 2026
        Assert.Equal("LEC Spring 2026", tournaments[0].Name);
        Assert.Equal(1, tournaments[0].LeagueId); // THIS WAS THE BUG!
    }

    [Fact]
    public async Task GetAllTournaments_FilterByStatus_ReturnsOnlyOngoingTournaments()
    {
        // Act - Filter by Ongoing status
        var response = await _client.GetAsync("/api/tournaments?status=Ongoing");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var tournaments = await response.Content.ReadFromJsonAsync<List<TournamentListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(tournaments);
        Assert.Equal(2, tournaments.Count); // LEC Spring & LCS Spring
        Assert.All(tournaments, t => Assert.Equal("Ongoing", t.Status.ToString()));
    }

    [Fact]
    public async Task GetAllTournaments_FilterByLeagueAndStatus_ReturnsCombinedFilter()
    {
        // Act - Filter by LEC and Ongoing
        var response = await _client.GetAsync("/api/tournaments?leagueId=1&status=Ongoing");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var tournaments = await response.Content.ReadFromJsonAsync<List<TournamentListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(tournaments);
        Assert.Single(tournaments);
        Assert.Equal("LEC Spring 2026", tournaments[0].Name);
    }

    [Fact]
    public async Task GetTournamentById_ReturnsOk_WithTournamentDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/tournaments/1");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var tournament = await response.Content.ReadFromJsonAsync<TournamentDetailsDto>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(tournament);
        Assert.Equal(1, tournament.Id);
        Assert.Equal("LEC Spring 2026", tournament.Name);
        Assert.NotNull(tournament.League);
        Assert.NotEmpty(tournament.Matches); // Should have 3 LEC matches
    }

    [Fact]
    public async Task GetTournamentById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/tournaments/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}