using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class BaseProductConfiguration : IEntityTypeConfiguration<BaseProduct>
{
    public void Configure(EntityTypeBuilder<BaseProduct> builder)
    {
        builder.ToTable("Base_Product");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.Name, e.IsDeleted });
        builder.HasIndex(e => e.CategoryId);
        builder.HasIndex(e => e.Brand);
        builder.HasIndex(e => e.BarCode);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Spec).HasMaxLength(200);
        builder.Property(e => e.Brand).HasMaxLength(100);
        builder.Property(e => e.Unit).HasMaxLength(20);
        builder.Property(e => e.BarCode).HasMaxLength(100);
        builder.Property(e => e.PurchasePrice).HasPrecision(18, 2);
        builder.Property(e => e.SalePrice).HasPrecision(18, 2);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
