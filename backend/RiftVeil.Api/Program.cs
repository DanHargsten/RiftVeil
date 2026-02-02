using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Infrastructure.Services.Read;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Development tooling for API discovery/testing.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as stings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        
        // Make JSON more readable in development
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services for reading data from the database.
builder.Services.AddScoped<ILeagueReadService, LeagueReadService>();
builder.Services.AddScoped<ITournamentReadService, TournamentReadService>();
builder.Services.AddScoped<IMatchReadService, MatchReadService>();

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
    app.UseCors();

    // Initialize database
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<RiftVeilDbContext>();

    // Only run migrations if using SQL Server
    if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        context.Database.Migrate();
    }
    
    DbInitializer.Initialize(context);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }