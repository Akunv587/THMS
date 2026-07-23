using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SaleReturnDetailConfiguration : BaseEntityConfiguration<SaleReturnDetail>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SaleReturnDetail> builder)
    {
        builder.ToTable("Sale_ReturnDetail");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.SaleOutDetailId);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.HasOne(e => e.SaleReturn).WithMany(o => o.Details).HasForeignKey(e => e.SaleReturnId);
        builder.HasOne(e => e.SaleOutDetail).WithMany().HasForeignKey(e => e.SaleOutDetailId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.NoAction);
    }
}
