using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class ImageEntityTypeConfiguration: IEntityTypeConfiguration<ImageEntity>
{
    public void Configure(EntityTypeBuilder<ImageEntity> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.PictureFileName).IsRequired().HasMaxLength(512);
        builder.Property(i => i.LastUsed).IsRequired();
        builder.Property(i => i.IsActive).IsRequired();

        builder.HasOne(i => i.Owner)
            .WithMany(u => u.ImageEntities)
            .HasForeignKey(i => i.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}