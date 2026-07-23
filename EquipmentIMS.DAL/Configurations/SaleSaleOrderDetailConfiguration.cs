using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SaleSaleOrderDetailConfiguration : BaseEntityConfiguration<SaleSaleOrderDetail>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SaleSaleOrderDetail> builder)
    {
        builder.ToTable("Sale_SaleOrderDetail");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.SaleOrderId);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Remark).HasMaxLength(200);
        builder.HasOne(e => e.SaleOrder).WithMany(o => o.Details).HasForeignKey(e => e.SaleOrderId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
    }
}
