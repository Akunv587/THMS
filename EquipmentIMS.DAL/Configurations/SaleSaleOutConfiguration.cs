using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SaleSaleOutConfiguration : IEntityTypeConfiguration<SaleSaleOut>
{
    public void Configure(EntityTypeBuilder<SaleSaleOut> builder)
    {
        builder.ToTable("Sale_SaleOut");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.OutNo).IsUnique();
        builder.HasIndex(e => e.SaleOrderId);
        builder.Property(e => e.OutNo).HasMaxLength(50);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.SaleOrder).WithMany().HasForeignKey(e => e.SaleOrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
        builder.HasMany(e => e.Details).WithOne(d => d.SaleOut).HasForeignKey(d => d.SaleOutId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
