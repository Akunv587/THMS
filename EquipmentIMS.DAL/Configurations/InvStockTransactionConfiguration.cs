using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class InvStockTransactionConfiguration : IEntityTypeConfiguration<InvStockTransaction>
{
    public void Configure(EntityTypeBuilder<InvStockTransaction> builder)
    {
        builder.ToTable("Inv_StockTransaction");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TransactionNo).IsUnique();
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.WarehouseId);
        builder.HasIndex(e => e.TransactionType);
        builder.HasIndex(e => e.CreateTime);
        builder.HasIndex(e => e.RefNo);
        builder.Property(e => e.TransactionNo).HasMaxLength(50);
        builder.Property(e => e.RefType).HasMaxLength(50);
        builder.Property(e => e.RefNo).HasMaxLength(50);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.Property(e => e.CreateBy).HasMaxLength(50);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
    }
}
