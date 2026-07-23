using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysUserRoleConfiguration : IEntityTypeConfiguration<SysUserRole>
{
    public void Configure(EntityTypeBuilder<SysUserRole> builder)
    {
        builder.ToTable("Sys_UserRole");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
        builder.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
        builder.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
    }
}
