using System.Net;
using System.Net.Http.Json;
using RiftVeil.Api.Tests.Infrastructure;
using RiftVeil.Application.Dtos.Leagues;

namespace RiftVeil.Api.Tests.Controllers;

[Collection("Database collection")]
public class LeaguesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LeaguesControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllLeagues_ReturnsOk_WithSeededLeagues()
    {
        // Act
        var response = await _client.GetAsync("/api/leagues");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var leagues = await response.Content.ReadFromJsonAsync<List<LeagueListItemDto>>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(leagues);
        Assert.Equal(3, leagues.Count); // LEC, LCS, INTL
        
        // Verify they're ordered by name
        Assert.Equal("League of Legends EMEA Championship", leagues[0].Name);
        Assert.Equal("League of Legends International Championship", leagues[1].Name);
        Assert.Equal("League of Legends North America Championship", leagues[2].Name);
    }

    [Fact]
    public async Task GetLeagueById_ReturnsOk_WithLeagueDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/leagues/1");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var league = await response.Content.ReadFromJsonAsync<LeagueDetailsDto>(
            TestWebApplicationFactory.GetJsonSerializerOptions());
        Assert.NotNull(league);
        Assert.Equal(1, league.Id);
        Assert.Equal("LEC", league.ShortName);
        Assert.NotEmpty(league.Tournaments); // Should have LEC Spring 2026
    }

    [Fact]
    public async Task GetLeagueById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/leagues/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}