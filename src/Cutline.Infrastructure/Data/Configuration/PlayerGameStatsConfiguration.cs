namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlayerGameStatsConfiguration : IEntityTypeConfiguration<PlayerGameStats>
{
    public void Configure(EntityTypeBuilder<PlayerGameStats> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.GameStatus).HasConversion<string>();
        builder.Property(s => s.Points).HasColumnType("decimal(8,2)");

        builder.HasOne(s => s.Player)
               .WithMany()
               .HasForeignKey(s => s.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        // One record per player per week per season
        builder.HasIndex(s => new { s.PlayerId, s.Season, s.WeekNumber }).IsUnique();
    }
}
