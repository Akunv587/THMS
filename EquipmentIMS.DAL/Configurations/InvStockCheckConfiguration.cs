using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class InvStockCheckConfiguration : IEntityTypeConfiguration<InvStockCheck>
{
    public void Configure(EntityTypeBuilder<InvStockCheck> builder)
    {
        builder.ToTable("Inv_StockCheck");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.CheckNo).IsUnique();
        builder.Property(e => e.CheckNo).HasMaxLength(50);
        builder.Property(e => e.TotalDiffAmount).HasPrecision(18, 2);
        builder.Property(e => e.Approver).HasMaxLength(50);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
        builder.HasMany(e => e.Details).WithOne(d => d.StockCheck).HasForeignKey(d => d.StockCheckId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
