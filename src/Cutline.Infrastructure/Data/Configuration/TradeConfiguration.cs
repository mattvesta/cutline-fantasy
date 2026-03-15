namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.InitiatorTeam)
            .WithMany()
            .HasForeignKey(t => t.InitiatorTeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.ReceiverTeam)
            .WithMany()
            .HasForeignKey(t => t.ReceiverTeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.Trade)
            .HasForeignKey(i => i.TradeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Message).HasMaxLength(500);

        builder.HasIndex(t => t.LeagueId);
        builder.HasIndex(t => new { t.InitiatorTeamId, t.Status });
        builder.HasIndex(t => new { t.ReceiverTeamId, t.Status });
    }
}

public class TradeItemConfiguration : IEntityTypeConfiguration<TradeItem>
{
    public void Configure(EntityTypeBuilder<TradeItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Player)
            .WithMany()
            .HasForeignKey(i => i.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
