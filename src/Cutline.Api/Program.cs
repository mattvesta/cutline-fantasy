using Cutline.Api.Endpoints;
using Cutline.Api.Hubs;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Cutline.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CutlineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<ILeagueRepository, LeagueRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IWeekRepository, WeekRepository>();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapLeagues();
app.MapTeams();

if (app.Environment.IsDevelopment())
    app.MapDevEndpoints();

app.MapHub<ScoringHub>("/hubs/scoring");

app.Run();
