namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);

        builder.ComplexProperty(l => l.ScoringSettings);
        builder.ComplexProperty(l => l.RosterSettings);

        builder.HasMany(l => l.Teams)
            .WithOne(t => t.League)
            .HasForeignKey(t => t.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Weeks)
            .WithOne(w => w.League)
            .HasForeignKey(w => w.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
