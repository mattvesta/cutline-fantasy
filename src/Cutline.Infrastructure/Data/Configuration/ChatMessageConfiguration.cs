namespace Cutline.Infrastructure.Data.Configuration;

using Cutline.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Manager)
            .WithMany()
            .HasForeignKey(m => m.ManagerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.Content).HasMaxLength(2000);
        builder.Property(m => m.GifUrl).HasMaxLength(1000);

        // Primary query pattern: paginate by league + time
        builder.HasIndex(m => new { m.LeagueId, m.SentAt });
    }
}
