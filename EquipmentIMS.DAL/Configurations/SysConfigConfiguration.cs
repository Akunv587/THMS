using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysConfigConfiguration : IEntityTypeConfiguration<SysConfig>
{
    public void Configure(EntityTypeBuilder<SysConfig> builder)
    {
        builder.ToTable("Sys_Config");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ConfigKey).IsUnique();
        builder.Property(e => e.ConfigKey).HasMaxLength(100);
        builder.Property(e => e.ConfigValue).HasMaxLength(2000);
        builder.Property(e => e.ConfigName).HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
