namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TeamScoreConfiguration : IEntityTypeConfiguration<TeamScore>
{
    public void Configure(EntityTypeBuilder<TeamScore> builder)
    {
        builder.HasKey(ts => ts.Id);
        builder.HasIndex(ts => new { ts.WeekId, ts.TeamId }).IsUnique();

        builder.HasOne(ts => ts.Team)
            .WithMany()
            .HasForeignKey(ts => ts.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
