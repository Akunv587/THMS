using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IRepository<BaseWarehouse> _repo;

    public WarehouseService(IUnitOfWork uow)
    {
        _repo = uow.GetRepository<BaseWarehouse>();
    }

    public async Task<PagedResult<WarehouseDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var result = await _repo.GetPagedAsync(page, pageSize, string.IsNullOrWhiteSpace(keyword) ? null :
            w => (!w.IsDeleted) && (w.Code.Contains(keyword) || w.Name.Contains(keyword)));

        return new PagedResult<WarehouseDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<List<WarehouseDto>> GetAllAsync()
    {
        var entities = await _repo.FindAsync(w => !w.IsDeleted);
        return entities.Select(MapToDto).ToList();
    }

    public async Task<WarehouseDto?> GetByIdAsync(long id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity == null || entity.IsDeleted ? null : MapToDto(entity);
    }

    public async Task<WarehouseDto> CreateAsync(WarehouseDto dto)
    {
        if (dto.IsDefault)
        {
            await _repo.Query().Where(w => !w.IsDeleted && w.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
        }
        var entity = new BaseWarehouse
        {
            Code = dto.Code,
            Name = dto.Name,
            Address = dto.Address,
            Manager = dto.Manager,
            Phone = dto.Phone,
            IsDefault = dto.IsDefault
        };
        await _repo.AddAsync(entity);
        return MapToDto(entity);
    }

    public async Task UpdateAsync(WarehouseDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return;
        if (dto.IsDefault)
        {
            await _repo.Query().Where(w => !w.IsDeleted && w.IsDefault && w.Id != dto.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
        }
        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.Address = dto.Address;
        entity.Manager = dto.Manager;
        entity.Phone = dto.Phone;
        entity.IsDefault = dto.IsDefault;
        _repo.Update(entity);
    }

    public async Task DeleteAsync(long id)
    {
        var hasStock = await _repo.Query().AnyAsync(w => w.Id == id && w.ProductStocks.Any(s => !s.IsDeleted));
        if (hasStock) throw new InvalidOperationException("该仓库下存在库存，无法删除");
        await _repo.Query().Where(w => w.Id == id && !w.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true));
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
    {
        var query = _repo.Query().Where(w => !w.IsDeleted && w.Code == code);
        if (excludeId.HasValue) query = query.Where(w => w.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    private static WarehouseDto MapToDto(BaseWarehouse e) => new()
    {
        Id = e.Id, Code = e.Code, Name = e.Name, Address = e.Address,
        Manager = e.Manager, Phone = e.Phone, IsDefault = e.IsDefault, CreateTime = e.CreateTime
    };
}
