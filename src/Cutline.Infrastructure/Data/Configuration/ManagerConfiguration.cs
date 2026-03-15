namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ManagerConfiguration : IEntityTypeConfiguration<Manager>
{
    public void Configure(EntityTypeBuilder<Manager> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Email).IsRequired().HasMaxLength(254);
        builder.Property(m => m.AvatarUrl).HasMaxLength(500);

        builder.HasIndex(m => m.Email).IsUnique();
    }
}

public class LeagueManagerConfiguration : IEntityTypeConfiguration<LeagueManager>
{
    public void Configure(EntityTypeBuilder<LeagueManager> builder)
    {
        builder.HasKey(lm => new { lm.LeagueId, lm.ManagerId });

        builder.HasOne(lm => lm.League)
            .WithMany(l => l.LeagueManagers)
            .HasForeignKey(lm => lm.LeagueId);

        builder.HasOne(lm => lm.Manager)
            .WithMany(m => m.LeagueManagers)
            .HasForeignKey(lm => lm.ManagerId);

        // Ignore the computed Team property — it's a convenience nav, not mapped
        builder.Ignore(lm => lm.Team);
    }
}
