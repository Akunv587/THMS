using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class BaseSupplierConfiguration : BaseEntityConfiguration<BaseSupplier>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BaseSupplier> builder)
    {
        builder.ToTable("Base_Supplier");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => new { e.Name, e.IsDeleted });
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.ContactPerson).HasMaxLength(50);
        builder.Property(e => e.ContactPhone).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.BankAccount).HasMaxLength(500);
        builder.Property(e => e.BankName).HasMaxLength(100);
        builder.Property(e => e.Remark).HasMaxLength(500);
    }
}
