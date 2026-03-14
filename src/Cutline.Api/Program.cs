using Cutline.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CutlineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// TODO: Register repositories and services

var app = builder.Build();

app.UseHttpsRedirection();

// TODO: Map endpoint groups
// TODO: Map SignalR hubs

app.Run();
