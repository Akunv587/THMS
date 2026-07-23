using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysOperationLogConfiguration : IEntityTypeConfiguration<SysOperationLog>
{
    public void Configure(EntityTypeBuilder<SysOperationLog> builder)
    {
        builder.ToTable("Sys_OperationLog");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserName).HasMaxLength(50);
        builder.Property(e => e.Module).HasMaxLength(50);
        builder.Property(e => e.Action).HasMaxLength(50);
        builder.Property(e => e.TargetType).HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.IP).HasMaxLength(50);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreateTime);
        builder.HasIndex(e => e.Module);
    }
}
