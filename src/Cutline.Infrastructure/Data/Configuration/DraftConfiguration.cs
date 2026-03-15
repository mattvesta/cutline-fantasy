namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DraftConfiguration : IEntityTypeConfiguration<Draft>
{
    public void Configure(EntityTypeBuilder<Draft> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Status).HasConversion<string>();

        builder.HasOne(d => d.League)
               .WithMany()
               .HasForeignKey(d => d.LeagueId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Picks)
               .WithOne(p => p.Draft)
               .HasForeignKey(p => p.DraftId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DraftPickConfiguration : IEntityTypeConfiguration<DraftPick>
{
    public void Configure(EntityTypeBuilder<DraftPick> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Team)
               .WithMany()
               .HasForeignKey(p => p.TeamId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Player)
               .WithMany()
               .HasForeignKey(p => p.PlayerId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.DraftId, p.PickNumber }).IsUnique();
    }
}
