using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class InvStockTransferDetailConfiguration : BaseEntityConfiguration<InvStockTransferDetail>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InvStockTransferDetail> builder)
    {
        builder.ToTable("Inv_StockTransferDetail");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Remark).HasMaxLength(200);
        builder.HasOne(e => e.Transfer).WithMany(o => o.Details).HasForeignKey(e => e.TransferId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
    }
}
