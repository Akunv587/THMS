using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class PurPurchaseOrderDetailConfiguration : BaseEntityConfiguration<PurPurchaseOrderDetail>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PurPurchaseOrderDetail> builder)
    {
        builder.ToTable("Pur_PurchaseOrderDetail");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.PurchaseOrderId);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Remark).HasMaxLength(200);
        builder.HasOne(e => e.PurchaseOrder).WithMany(o => o.Details).HasForeignKey(e => e.PurchaseOrderId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
    }
}
