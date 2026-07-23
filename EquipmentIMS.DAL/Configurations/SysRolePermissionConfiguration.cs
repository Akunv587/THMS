using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysRolePermissionConfiguration : IEntityTypeConfiguration<SysRolePermission>
{
    public void Configure(EntityTypeBuilder<SysRolePermission> builder)
    {
        builder.ToTable("Sys_RolePermission");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        builder.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId);
        builder.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId);
    }
}
