using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class MessageMetadataEntityTypeConfiguration: IEntityTypeConfiguration<MessageMetadata>
{
    public void Configure(EntityTypeBuilder<MessageMetadata> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.SentAt).IsRequired();
        builder.Property(m => m.MessageStatus).IsRequired().HasMaxLength(50);

        builder.HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}