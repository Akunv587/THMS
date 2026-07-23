using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class MaintRepairPartConfiguration : BaseEntityConfiguration<MaintRepairPart>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MaintRepairPart> builder)
    {
        builder.ToTable("Maint_RepairPart");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.RepairOrderId);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Remark).HasMaxLength(200);
        builder.HasOne(e => e.RepairOrder).WithMany(o => o.RepairParts).HasForeignKey(e => e.RepairOrderId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
    }
}
