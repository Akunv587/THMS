using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class InvStockAlertConfiguration : IEntityTypeConfiguration<InvStockAlert>
{
    public void Configure(EntityTypeBuilder<InvStockAlert> builder)
    {
        builder.ToTable("Inv_StockAlert");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.IsResolved);
        builder.HasIndex(e => e.CreateTime);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
    }
}
