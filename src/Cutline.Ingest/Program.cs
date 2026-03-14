using Cutline.Infrastructure.Data;
using Cutline.Ingest.Sports;
using Cutline.Ingest.Workers;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<CutlineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddHttpClient<SleeperClient>();
builder.Services.AddHttpClient<NflverseClient>();
builder.Services.AddHttpClient<EspnLiveScoringClient>();

// TODO: register IPlayerRepository

builder.Services.AddHostedService<SleeperSyncWorker>();
builder.Services.AddHostedService<NflverseRosterSyncWorker>();
builder.Services.AddHostedService<NflverseFinalStatsWorker>();
builder.Services.AddHostedService<EspnLiveScoringWorker>();

var host = builder.Build();
host.Run();
