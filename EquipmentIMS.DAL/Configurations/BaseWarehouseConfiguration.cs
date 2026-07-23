using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class BaseWarehouseConfiguration : IEntityTypeConfiguration<BaseWarehouse>
{
    public void Configure(EntityTypeBuilder<BaseWarehouse> builder)
    {
        builder.ToTable("Base_Warehouse");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Name).HasMaxLength(100);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.Manager).HasMaxLength(50);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
