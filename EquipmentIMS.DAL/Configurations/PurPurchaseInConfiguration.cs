using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class PurPurchaseInConfiguration : IEntityTypeConfiguration<PurPurchaseIn>
{
    public void Configure(EntityTypeBuilder<PurPurchaseIn> builder)
    {
        builder.ToTable("Pur_PurchaseIn");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.InNo).IsUnique();
        builder.Property(e => e.InNo).HasMaxLength(50);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.PurchaseOrder).WithMany().HasForeignKey(e => e.PurchaseOrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
        builder.HasMany(e => e.Details).WithOne(d => d.PurchaseIn).HasForeignKey(d => d.PurchaseInId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
