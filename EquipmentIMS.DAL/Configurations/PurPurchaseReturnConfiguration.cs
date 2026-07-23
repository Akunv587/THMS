using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class PurPurchaseReturnConfiguration : BaseEntityConfiguration<PurPurchaseReturn>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PurPurchaseReturn> builder)
    {
        builder.ToTable("Pur_PurchaseReturn");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ReturnNo).IsUnique();
        builder.Property(e => e.ReturnNo).HasMaxLength(50);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.PurchaseIn).WithMany().HasForeignKey(e => e.PurchaseInId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
    }
}
