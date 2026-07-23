using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Configurations;

/// <summary>
/// 实体配置基类，统一添加软删除过滤器
/// </summary>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
{
    public void Configure(EntityTypeBuilder<T> builder)
    {
        ConfigureEntity(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);
}
