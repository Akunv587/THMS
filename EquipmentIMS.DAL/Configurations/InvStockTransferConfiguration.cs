using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class InvStockTransferConfiguration : IEntityTypeConfiguration<InvStockTransfer>
{
    public void Configure(EntityTypeBuilder<InvStockTransfer> builder)
    {
        builder.ToTable("Inv_StockTransfer");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TransferNo).IsUnique();
        builder.Property(e => e.TransferNo).HasMaxLength(50);
        builder.Property(e => e.Approver).HasMaxLength(50);
        builder.Property(e => e.RejectReason).HasMaxLength(500);
        builder.Property(e => e.Operator).HasMaxLength(50);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.FromWarehouse).WithMany().HasForeignKey(e => e.FromWarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(e => e.ToWarehouse).WithMany().HasForeignKey(e => e.ToWarehouseId).OnDelete(DeleteBehavior.NoAction);
        builder.HasMany(e => e.Details).WithOne(d => d.Transfer).HasForeignKey(d => d.TransferId);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
