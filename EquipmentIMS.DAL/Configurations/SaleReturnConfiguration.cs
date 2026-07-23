using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.ToTable("Sale_Return");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.ReturnNo).IsUnique();
        builder.Property(e => e.ReturnNo).HasMaxLength(50);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.Approver).HasMaxLength(50);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.SaleOut).WithMany().HasForeignKey(e => e.SaleOutId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
        builder.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId);
        builder.HasMany(e => e.Details).WithOne(d => d.SaleReturn).HasForeignKey(d => d.SaleReturnId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
