using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class InvStockCheckDetailConfiguration : IEntityTypeConfiguration<InvStockCheckDetail>
{
    public void Configure(EntityTypeBuilder<InvStockCheckDetail> builder)
    {
        builder.ToTable("Inv_StockCheckDetail");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UnitPrice).HasPrecision(18, 2);
        builder.Property(e => e.Remark).HasMaxLength(200);
        builder.HasOne(e => e.StockCheck).WithMany(o => o.Details).HasForeignKey(e => e.StockCheckId);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
