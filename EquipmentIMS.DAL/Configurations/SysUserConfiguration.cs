using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysUserConfiguration : IEntityTypeConfiguration<SysUser>
{
    public void Configure(EntityTypeBuilder<SysUser> builder)
    {
        builder.ToTable("Sys_User");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserName).IsUnique();
        builder.Property(e => e.UserName).HasMaxLength(50);
        builder.Property(e => e.RealName).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.PasswordHash).HasMaxLength(500);
        builder.Property(e => e.PasswordSalt).HasMaxLength(100);
        builder.Property(e => e.LastLoginIP).HasMaxLength(50);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
