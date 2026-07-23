using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysPermissionConfiguration : IEntityTypeConfiguration<SysPermission>
{
    public void Configure(EntityTypeBuilder<SysPermission> builder)
    {
        builder.ToTable("Sys_Permission");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.PermissionCode).IsUnique();
        builder.Property(e => e.PermissionCode).HasMaxLength(100);
        builder.Property(e => e.PermissionName).HasMaxLength(100);
        builder.Property(e => e.Url).HasMaxLength(200);
        builder.Property(e => e.Icon).HasMaxLength(50);
        builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
