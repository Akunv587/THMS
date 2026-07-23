using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class BaseProductStockConfiguration : IEntityTypeConfiguration<BaseProductStock>
{
    public void Configure(EntityTypeBuilder<BaseProductStock> builder)
    {
        builder.ToTable("Base_ProductStock");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();
        builder.HasIndex(e => new { e.ProductId, e.WarehouseId });
        builder.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne(e => e.Product).WithMany(p => p.ProductStocks).HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
