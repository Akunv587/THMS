using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class BaseCustomerConfiguration : IEntityTypeConfiguration<BaseCustomer>
{
    public void Configure(EntityTypeBuilder<BaseCustomer> builder)
    {
        builder.ToTable("Base_Customer");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.CreateBy);
        builder.HasIndex(e => new { e.Name, e.IsDeleted });
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.ContactPerson).HasMaxLength(50);
        builder.Property(e => e.ContactPhone).HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.CreditLimit).HasPrecision(18, 2);
        builder.Property(e => e.UsedCredit).HasPrecision(18, 2);
        builder.Property(e => e.Remark).HasMaxLength(500);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
