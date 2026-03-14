namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class WeekConfiguration : IEntityTypeConfiguration<Week>
{
    public void Configure(EntityTypeBuilder<Week> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasIndex(w => new { w.LeagueId, w.WeekNumber }).IsUnique();

        builder.HasOne(w => w.EliminatedTeam)
            .WithMany()
            .HasForeignKey(w => w.EliminatedTeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(w => w.TeamScores)
            .WithOne(ts => ts.Week)
            .HasForeignKey(ts => ts.WeekId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.WaiverClaims)
            .WithOne(wc => wc.Week)
            .HasForeignKey(wc => wc.WeekId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
