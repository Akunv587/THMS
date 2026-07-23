using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class MaintDeviceArchiveConfiguration : BaseEntityConfiguration<MaintDeviceArchive>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MaintDeviceArchive> builder)
    {
        builder.ToTable("Maint_DeviceArchive");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.SerialNo).IsUnique();
        builder.HasIndex(e => e.CustomerId);
        builder.Property(e => e.SerialNo).HasMaxLength(100);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
        builder.HasOne(e => e.SaleOrder).WithMany().HasForeignKey(e => e.SaleOrderId);
    }
}
