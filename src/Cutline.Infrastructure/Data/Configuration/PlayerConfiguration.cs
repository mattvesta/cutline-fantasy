namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.GsisId).HasMaxLength(20);
        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(50);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Position).IsRequired().HasMaxLength(10);
        builder.Property(p => p.NflTeam).HasMaxLength(5);
        builder.Property(p => p.SleeperId).HasMaxLength(20);
        builder.Property(p => p.EspnId).HasMaxLength(20);
        builder.Property(p => p.College).HasMaxLength(100);
        builder.Property(p => p.Height).HasMaxLength(10);
        builder.Property(p => p.Weight).HasMaxLength(10);
        builder.Property(p => p.Adp).HasPrecision(7, 2);

        builder.HasIndex(p => p.GsisId).IsUnique();
        builder.HasIndex(p => p.SleeperId);
        builder.HasIndex(p => p.EspnId);
    }
}
