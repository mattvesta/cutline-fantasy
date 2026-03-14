namespace Cutline.Infrastructure.Data;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;

public class CutlineDbContext : DbContext
{
    public CutlineDbContext(DbContextOptions<CutlineDbContext> options) : base(options) { }

    public DbSet<League> Leagues => Set<League>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Week> Weeks => Set<Week>();
    public DbSet<TeamScore> TeamScores => Set<TeamScore>();
    public DbSet<RosterSlot> RosterSlots => Set<RosterSlot>();
    public DbSet<WaiverClaim> WaiverClaims => Set<WaiverClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CutlineDbContext).Assembly);
    }
}
