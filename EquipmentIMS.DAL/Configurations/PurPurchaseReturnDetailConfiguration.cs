using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class PurPurchaseReturnDetailConfiguration : BaseEntityConfiguration<PurPurchaseReturnDetail>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PurPurchaseReturnDetail> builder)
    {
        builder.ToTable("Pur_PurchaseReturnDetail");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.HasOne(e => e.PurchaseReturn).WithMany().HasForeignKey(e => e.PurchaseReturnId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
    }
}
