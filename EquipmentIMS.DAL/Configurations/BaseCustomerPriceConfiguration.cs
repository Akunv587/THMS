using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class BaseCustomerPriceConfiguration : BaseEntityConfiguration<BaseCustomerPrice>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BaseCustomerPrice> builder)
    {
        builder.ToTable("Base_CustomerPrice");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.CustomerId, e.ProductId }).IsUnique();
        builder.Property(e => e.SalePrice).HasPrecision(18, 2);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
    }
}
