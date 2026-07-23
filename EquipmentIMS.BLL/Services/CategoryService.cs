using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<BaseCategory> _repo;
    private readonly ICacheService _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public CategoryService(IUnitOfWork uow, ICacheService cache)
    {
        _repo = uow.GetRepository<BaseCategory>();
        _cache = cache;
    }

    public async Task<List<CategoryTreeDto>> GetTreeAsync()
    {
        var cached = await _cache.GetAsync<List<CategoryTreeDto>>("CategoryTree");
        if (cached != null) return cached;

        var all = await _repo.Query()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Level).ThenBy(c => c.SortOrder)
            .ToListAsync();

        var tree = BuildTree(all, null);
        await _cache.SetAsync("CategoryTree", tree, CacheDuration);
        return tree;
    }

    public async Task<CategoryDto?> GetByIdAsync(long id)
    {
        var e = await _repo.GetByIdAsync(id);
        return e == null || e.IsDeleted ? null : new CategoryDto
        {
            Id = e.Id, ParentId = e.ParentId, Code = e.Code,
            Name = e.Name, Level = e.Level, SortOrder = e.SortOrder
        };
    }

    public async Task<CategoryDto> CreateAsync(CategoryDto dto)
    {
        var entity = new BaseCategory
        {
            ParentId = dto.ParentId,
            Code = dto.Code,
            Name = dto.Name,
            Level = dto.ParentId.HasValue ? 2 : 1,
            SortOrder = dto.SortOrder
        };
        await _repo.AddAsync(entity);
        await _cache.RemoveAsync("CategoryTree");
        return new CategoryDto { Id = entity.Id, ParentId = entity.ParentId, Code = entity.Code, Name = entity.Name, Level = entity.Level };
    }

    public async Task UpdateAsync(CategoryDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return;
        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.SortOrder = dto.SortOrder;
        _repo.Update(entity);
        await _cache.RemoveAsync("CategoryTree");
    }

    public async Task DeleteAsync(long id)
    {
        var hasChildren = await HasChildrenAsync(id);
        if (hasChildren) throw new InvalidOperationException("该分类下存在子分类，无法删除");
        var hasProducts = await _repo.Query().AnyAsync(c => c.Id == id && c.Products.Any(p => !p.IsDeleted));
        if (hasProducts) throw new InvalidOperationException("该分类下存在设备，无法删除");
        await _repo.Query().Where(c => c.Id == id && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
        await _cache.RemoveAsync("CategoryTree");
    }

    public async Task<bool> HasChildrenAsync(long id)
    {
        return await _repo.Query().AnyAsync(c => !c.IsDeleted && c.ParentId == id);
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
    {
        var query = _repo.Query().Where(c => !c.IsDeleted && c.Code == code);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    private static List<CategoryTreeDto> BuildTree(List<BaseCategory> all, long? parentId)
    {
        return all.Where(c => c.ParentId == parentId).Select(c => new CategoryTreeDto
        {
            Id = c.Id, Code = c.Code, Name = c.Name, Level = c.Level,
            Children = BuildTree(all, c.Id)
        }).ToList();
    }
}
