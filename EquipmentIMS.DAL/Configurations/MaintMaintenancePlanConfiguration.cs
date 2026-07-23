using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class MaintMaintenancePlanConfiguration : BaseEntityConfiguration<MaintMaintenancePlan>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MaintMaintenancePlan> builder)
    {
        builder.ToTable("Maint_MaintenancePlan");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.NextDate);
        builder.Property(e => e.PlanName).HasMaxLength(200);
        builder.Property(e => e.Content).HasMaxLength(2000);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.DeviceArchive).WithMany().HasForeignKey(e => e.DeviceArchiveId);
    }
}
