using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SaleSaleOrderConfiguration : IEntityTypeConfiguration<SaleSaleOrder>
{
    public void Configure(EntityTypeBuilder<SaleSaleOrder> builder)
    {
        builder.ToTable("Sale_SaleOrder");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.OrderNo).IsUnique();
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreateBy);
        builder.Property(e => e.OrderNo).HasMaxLength(50);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.ShippedAmount).HasPrecision(18, 2);
        builder.Property(e => e.Approver).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
        builder.HasMany(e => e.Details).WithOne(d => d.SaleOrder).HasForeignKey(d => d.SaleOrderId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
