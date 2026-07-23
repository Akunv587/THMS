using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class PurPurchaseOrderConfiguration : IEntityTypeConfiguration<PurPurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurPurchaseOrder> builder)
    {
        builder.ToTable("Pur_PurchaseOrder");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.OrderNo).IsUnique();
        builder.HasIndex(e => e.SupplierId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.OrderDate);
        builder.Property(e => e.OrderNo).HasMaxLength(50);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.ReceivedAmount).HasPrecision(18, 2);
        builder.Property(e => e.Approver).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
        builder.HasMany(e => e.Details).WithOne(d => d.PurchaseOrder).HasForeignKey(d => d.PurchaseOrderId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
