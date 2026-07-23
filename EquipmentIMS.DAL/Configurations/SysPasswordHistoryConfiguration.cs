using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

public class SysPasswordHistoryConfiguration : IEntityTypeConfiguration<SysPasswordHistory>
{
    public void Configure(EntityTypeBuilder<SysPasswordHistory> builder)
    {
        builder.ToTable("Sys_PasswordHistory");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PasswordHash).HasMaxLength(500);
        builder.Property(e => e.PasswordSalt).HasMaxLength(100);
        builder.HasIndex(e => e.UserId);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
    }
}
