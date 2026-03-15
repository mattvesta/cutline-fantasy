using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Cutline.Api.Endpoints;
using Cutline.Api.Hubs;
using Cutline.Core.Entities;
using Cutline.Core.Interfaces;
using Cutline.Infrastructure.Data;
using Cutline.Infrastructure.Repositories;
using Cutline.Infrastructure.Services;
using Cutline.Infrastructure.Sports;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddHttpClient<NflverseClient>();
builder.Services.AddScoped<NflverseStatsImporter>();
builder.Services.AddScoped<IPasswordHasher<Manager>, PasswordHasher<Manager>>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var jwtKey    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

// Disable claim type remapping so "sub" stays "sub" in User.FindFirstValue
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = jwtKey,
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapLeagues();
app.MapTeams();
app.MapWeeks();
app.MapPlayers();
app.MapManagers();
app.MapDraftEndpoints();
app.MapScoringEndpoints();
app.MapWaiverEndpoints();
app.MapHistoryEndpoints();
app.MapTradeEndpoints();
app.MapCommissionerEndpoints();
app.MapChatEndpoints();

if (app.Environment.IsDevelopment())
    app.MapDevEndpoints();

app.MapHub<ScoringHub>("/hubs/scoring");
app.MapHub<DraftHub>("/hubs/draft");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
