namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.OwnerUserId).HasMaxLength(100);

        builder.HasOne(t => t.Manager)
            .WithMany(m => m.Teams)
            .HasForeignKey(t => t.ManagerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // One manager can only have one team per league
        builder.HasIndex(t => new { t.LeagueId, t.ManagerId })
            .IsUnique()
            .HasFilter("\"ManagerId\" IS NOT NULL");
    }
}
