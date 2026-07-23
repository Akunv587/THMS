using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysNotificationConfiguration : IEntityTypeConfiguration<SysNotification>
{
    public void Configure(EntityTypeBuilder<SysNotification> builder)
    {
        builder.ToTable("Sys_Notification");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Content).HasMaxLength(2000);
        builder.Property(e => e.RefType).HasMaxLength(50);
        builder.HasIndex(e => new { e.UserId, e.IsRead });
        builder.HasIndex(e => e.CreateTime);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
    }
}
