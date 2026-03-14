namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class WaiverClaimConfiguration : IEntityTypeConfiguration<WaiverClaim>
{
    public void Configure(EntityTypeBuilder<WaiverClaim> builder)
    {
        builder.HasKey(wc => wc.Id);

        builder.HasOne(wc => wc.Team)
            .WithMany()
            .HasForeignKey(wc => wc.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(wc => wc.AddPlayer)
            .WithMany()
            .HasForeignKey(wc => wc.AddPlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(wc => wc.DropPlayer)
            .WithMany()
            .HasForeignKey(wc => wc.DropPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(wc => wc.RejectionReason).HasMaxLength(200);
    }
}
