using System.Text.Json.Serialization;
using Cutline.Api.Endpoints;
using Cutline.Api.Hubs;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Cutline.Infrastructure.Repositories;
using Cutline.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CutlineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ILeagueRepository, LeagueRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IManagerRepository, ManagerRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IWeekRepository, WeekRepository>();
builder.Services.AddScoped<IGuillotineEngine, GuillotineEngine>();
builder.Services.AddScoped<IWaiverProcessor, WaiverProcessor>();
builder.Services.AddScoped<IDraftService, DraftService>();
builder.Services.AddScoped<ILiveScoringService, LiveScoringService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapLeagues();
app.MapTeams();
app.MapWeeks();
app.MapPlayers();
app.MapManagers();
app.MapDraftEndpoints();
app.MapScoringEndpoints();
app.MapWaiverEndpoints();

if (app.Environment.IsDevelopment())
    app.MapDevEndpoints();

app.MapHub<ScoringHub>("/hubs/scoring");
app.MapHub<DraftHub>("/hubs/draft");

app.Run();
