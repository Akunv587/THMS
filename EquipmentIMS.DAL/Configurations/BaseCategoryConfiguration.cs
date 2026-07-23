using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class BaseCategoryConfiguration : BaseEntityConfiguration<BaseCategory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BaseCategory> builder)
    {
        builder.ToTable("Base_Category");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.ParentId);
        builder.Property(e => e.Code).HasMaxLength(50);
        builder.Property(e => e.Name).HasMaxLength(100);
        builder.HasOne(e => e.Parent).WithMany(e => e.Children).HasForeignKey(e => e.ParentId);
    }
}
