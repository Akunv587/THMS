using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class MaintRepairOrderConfiguration : IEntityTypeConfiguration<MaintRepairOrder>
{
    public void Configure(EntityTypeBuilder<MaintRepairOrder> builder)
    {
        builder.ToTable("Maint_RepairOrder");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.OrderNo).IsUnique();
        builder.HasIndex(e => e.DeviceArchiveId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.TechnicianId);
        builder.Property(e => e.OrderNo).HasMaxLength(50);
        builder.Property(e => e.FaultDesc).HasMaxLength(1000);
        builder.Property(e => e.FaultType).HasMaxLength(100);
        builder.Property(e => e.TechnicianName).HasMaxLength(50);
        builder.Property(e => e.RepairDesc).HasMaxLength(2000);
        builder.Property(e => e.RepairCost).HasPrecision(18, 2);
        builder.Property(e => e.PartsCost).HasPrecision(18, 2);
        builder.Property(e => e.TotalCost).HasPrecision(18, 2);
        builder.Property(e => e.WorkHours).HasPrecision(5, 1);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.DeviceArchive).WithMany().HasForeignKey(e => e.DeviceArchiveId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
        builder.HasOne(e => e.Technician).WithMany().HasForeignKey(e => e.TechnicianId);
        builder.HasMany(e => e.RepairParts).WithOne(p => p.RepairOrder).HasForeignKey(p => p.RepairOrderId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
