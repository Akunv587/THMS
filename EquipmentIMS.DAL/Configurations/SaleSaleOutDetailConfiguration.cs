using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SaleSaleOutDetailConfiguration : IEntityTypeConfiguration<SaleSaleOutDetail>
{
    public void Configure(EntityTypeBuilder<SaleSaleOutDetail> builder)
    {
        builder.ToTable("Sale_SaleOutDetail");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.HasOne(e => e.SaleOut).WithMany(o => o.Details).HasForeignKey(e => e.SaleOutId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
