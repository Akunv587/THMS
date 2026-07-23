using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysRoleConfiguration : IEntityTypeConfiguration<SysRole>
{
    public void Configure(EntityTypeBuilder<SysRole> builder)
    {
        builder.ToTable("Sys_Role");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.RoleCode).IsUnique();
        builder.Property(e => e.RoleCode).HasMaxLength(50);
        builder.Property(e => e.RoleName).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(200);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
