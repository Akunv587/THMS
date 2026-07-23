using System.Linq.Expressions;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.DAL.Interfaces;

/// <summary>
/// 泛型仓储接口，约束仅限继承 BaseEntity 的业务实体
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(long id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();
    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}
