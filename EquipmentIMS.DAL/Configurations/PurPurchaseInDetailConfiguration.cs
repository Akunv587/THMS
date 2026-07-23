using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class PurPurchaseInDetailConfiguration : IEntityTypeConfiguration<PurPurchaseInDetail>
{
    public void Configure(EntityTypeBuilder<PurPurchaseInDetail> builder)
    {
        builder.ToTable("Pur_PurchaseInDetail");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.HasOne(e => e.PurchaseIn).WithMany(o => o.Details).HasForeignKey(e => e.PurchaseInId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
    }
}
