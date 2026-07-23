using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

public class CustomerService : ICustomerService
{
    private readonly IRepository<BaseCustomer> _repo;
    private readonly IRepository<BaseCustomerPrice> _priceRepo;
    private readonly ICacheService _cache;

    public CustomerService(IUnitOfWork uow, ICacheService cache)
    {
        _repo = uow.GetRepository<BaseCustomer>();
        _priceRepo = uow.GetRepository<BaseCustomerPrice>();
        _cache = cache;
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(int page, int pageSize, long userId, bool isManager, string? keyword = null)
    {
        System.Linq.Expressions.Expression<Func<BaseCustomer, bool>>? predicate;

        if (string.IsNullOrWhiteSpace(keyword))
        {
            if (!isManager)
                predicate = c => !c.IsDeleted && c.CreateBy == userId.ToString();
            else
                predicate = c => !c.IsDeleted;
        }
        else
        {
            if (!isManager)
                predicate = c => !c.IsDeleted && c.CreateBy == userId.ToString()
                    && (c.Code.Contains(keyword) || c.Name.Contains(keyword));
            else
                predicate = c => !c.IsDeleted
                    && (c.Code.Contains(keyword) || c.Name.Contains(keyword));
        }

        var result = await _repo.GetPagedAsync(page, pageSize, predicate);

        return PagedResult<CustomerDto>.Create(result.Items.Select(MapToDto).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<List<CustomerDto>> GetAllAsync(long userId, bool isManager, string? keyword = null)
    {
        var query = _repo.Query().Where(c => !c.IsDeleted);
        if (!isManager)
            query = query.Where(c => c.CreateBy == userId.ToString());
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(c => c.Code.Contains(keyword) || c.Name.Contains(keyword));
        var list = await query.OrderBy(c => c.Code).Take(200).ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(long id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity == null || entity.IsDeleted ? null : MapToDto(entity);
    }

    public async Task<CustomerDto> CreateAsync(CustomerDto dto)
    {
        var entity = new BaseCustomer
        {
            Code = dto.Code,
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone,
            Email = dto.Email,
            Address = dto.Address,
            CreditLimit = dto.CreditLimit,
            Remark = dto.Remark
        };
        await _repo.AddAsync(entity);
        return MapToDto(entity);
    }

    public async Task UpdateAsync(CustomerDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return;
        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.ContactPerson = dto.ContactPerson;
        entity.ContactPhone = dto.ContactPhone;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.CreditLimit = dto.CreditLimit;
        entity.Remark = dto.Remark;
        _repo.Update(entity);
    }

    public Task DeleteAsync(long id)
    {
        _repo.Query().Where(c => c.Id == id && !c.IsDeleted).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsDeleted, true));
        return Task.CompletedTask;
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
    {
        var query = _repo.Query().Where(c => !c.IsDeleted && c.Code == code);
        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<List<CustomerPriceDto>> GetPricesByCustomerAsync(long customerId)
    {
        return await _priceRepo.Query()
            .Where(p => !p.IsDeleted && p.CustomerId == customerId)
            .Include(p => p.Product)
            .Select(p => new CustomerPriceDto
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                ProductId = p.ProductId,
                ProductCode = p.Product.Code,
                ProductName = p.Product.Name,
                Spec = p.Product.Spec,
                SalePrice = p.SalePrice,
                EffectiveDate = p.EffectiveDate,
                ExpiryDate = p.ExpiryDate,
                Remark = p.Remark
            })
            .ToListAsync();
    }

    public async Task SetPriceAsync(CustomerPriceDto dto)
    {
        var entity = new BaseCustomerPrice
        {
            CustomerId = dto.CustomerId,
            ProductId = dto.ProductId,
            SalePrice = dto.SalePrice,
            EffectiveDate = dto.EffectiveDate,
            ExpiryDate = dto.ExpiryDate,
            Remark = dto.Remark
        };
        await _priceRepo.AddAsync(entity);
    }

    public async Task UpdatePriceAsync(CustomerPriceDto dto)
    {
        var entity = await _priceRepo.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return;
        entity.SalePrice = dto.SalePrice;
        entity.EffectiveDate = dto.EffectiveDate;
        entity.ExpiryDate = dto.ExpiryDate;
        entity.Remark = dto.Remark;
        _priceRepo.Update(entity);
    }

    public async Task DeletePriceAsync(long priceId)
    {
        var entity = await _priceRepo.GetByIdAsync(priceId);
        if (entity != null) _priceRepo.Remove(entity);
    }

    private static CustomerDto MapToDto(BaseCustomer e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        Name = e.Name,
        ContactPerson = e.ContactPerson,
        ContactPhone = e.ContactPhone,
        Email = e.Email,
        Address = e.Address,
        CreditLimit = e.CreditLimit,
        UsedCredit = e.UsedCredit,
        Remark = e.Remark,
        CreateTime = e.CreateTime
    };
}
