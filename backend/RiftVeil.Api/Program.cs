using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Infrastructure.Services.Import;
using RiftVeil.Infrastructure.Services.Read;

var builder = WebApplication.CreateBuilder(args);

// Development tooling for API discovery/testing.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Make JSON more readable in development.
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<LeaguepediaClientOptions>(
    builder.Configuration.GetSection(LeaguepediaClientOptions.SectionName));

// Leaguepedia API client. A single CookieContainer is shared across all
// requests so the MediaWiki session cookie set by bot login persists.
var leaguepediaCookies = new CookieContainer();
builder.Services.AddSingleton(leaguepediaCookies);

var leaguepediaContact = builder.Configuration
    .GetSection(LeaguepediaClientOptions.SectionName)
    .GetValue<string>(nameof(LeaguepediaClientOptions.ContactEmail));
var leaguepediaUserAgent = string.IsNullOrWhiteSpace(leaguepediaContact)
    ? "RiftVeil_Bot/1.1"
    : $"RiftVeil_Bot/1.1 (contact: {leaguepediaContact})";

builder.Services.AddHttpClient<LeaguepediaClient>(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", leaguepediaUserAgent);
        client.DefaultRequestHeaders.AcceptEncoding.Add(
            new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        UseCookies = true,
        CookieContainer = leaguepediaCookies
    });

// Lolesports VOD client (API key: Lolesports:ApiKey).
builder.Services.Configure<LolesportsClientOptions>(
    builder.Configuration.GetSection(LolesportsClientOptions.SectionName));
builder.Services.AddHttpClient<LolesportsClient>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36");
});

builder.Services.AddScoped<LeaguepediaTeamLogoVerifier>();

// Import and VOD enrichment (uses HTTP clients above).
builder.Services.AddScoped<LolesportsVodEnricher>();
builder.Services.AddScoped<LeaguepediaImportService>();
builder.Services.AddScoped<GameDetailImportService>();

// Read services for database access.
builder.Services.AddScoped<ILeagueReadService, LeagueReadService>();
builder.Services.AddScoped<ITournamentReadService, TournamentReadService>();
builder.Services.AddScoped<IMatchReadService, MatchReadService>();
builder.Services.AddScoped<IGameReadService, GameReadService>();

// Central data access for the API layer.
builder.Services.AddDbContext<RiftVeilDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Allow the local frontend dev server to call the API.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Initialize database.
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<RiftVeilDbContext>();

    // Only run migrations if using SQL Server.
    if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        context.Database.Migrate();
    }

    DbInitializer.Initialize(context);
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
