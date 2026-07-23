using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

public class SupplierService : ISupplierService
{
    private readonly IRepository<BaseSupplier> _repo;
    private readonly IAesEncryptionService _aes;

    public SupplierService(IUnitOfWork uow, IAesEncryptionService aes)
    {
        _repo = uow.GetRepository<BaseSupplier>();
        _aes = aes;
    }

    public async Task<PagedResult<SupplierDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var result = await _repo.GetPagedAsync(page, pageSize, string.IsNullOrWhiteSpace(keyword) ? null :
            s => (!s.IsDeleted) && (s.Code.Contains(keyword) || s.Name.Contains(keyword)));

        return new PagedResult<SupplierDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<List<SupplierDto>> GetAllAsync(string? keyword = null)
    {
        var query = _repo.Query().Where(s => !s.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(s => s.Code.Contains(keyword) || s.Name.Contains(keyword));
        var list = await query.OrderBy(s => s.Code).Take(200).ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto?> GetByIdAsync(long id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity == null || entity.IsDeleted ? null : MapToDto(entity);
    }

    public async Task<SupplierDto> CreateAsync(SupplierDto dto)
    {
        var entity = new BaseSupplier
        {
            Code = dto.Code,
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone,
            Email = dto.Email,
            Address = dto.Address,
            BankAccount = string.IsNullOrEmpty(dto.BankAccount) ? null : _aes.Encrypt(dto.BankAccount),
            BankName = dto.BankName,
            Remark = dto.Remark
        };
        await _repo.AddAsync(entity);
        return MapToDto(entity);
    }

    public async Task UpdateAsync(SupplierDto dto)
    {
        var entity = await _repo.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return;
        entity.Code = dto.Code;
        entity.Name = dto.Name;
        entity.ContactPerson = dto.ContactPerson;
        entity.ContactPhone = dto.ContactPhone;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.BankAccount = string.IsNullOrEmpty(dto.BankAccount) ? null : _aes.Encrypt(dto.BankAccount);
        entity.BankName = dto.BankName;
        entity.Remark = dto.Remark;
        _repo.Update(entity);
    }

    public Task DeleteAsync(long id)
    {
        _repo.Query().Where(s => s.Id == id && !s.IsDeleted).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsDeleted, true));
        return Task.CompletedTask;
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeId = null)
    {
        var query = _repo.Query().Where(s => !s.IsDeleted && s.Code == code);
        if (excludeId.HasValue) query = query.Where(s => s.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    private SupplierDto MapToDto(BaseSupplier e) => new()
    {
        Id = e.Id,
        Code = e.Code,
        Name = e.Name,
        ContactPerson = e.ContactPerson,
        ContactPhone = e.ContactPhone,
        Email = e.Email,
        Address = e.Address,
        BankAccount = e.BankAccount,
        BankAccountMasked = string.IsNullOrEmpty(e.BankAccount) ? null : MaskBankAccount(e.BankAccount),
        BankName = e.BankName,
        Remark = e.Remark,
        CreateTime = e.CreateTime
    };

    private string MaskBankAccount(string? encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return string.Empty;
        try
        {
            var decrypted = _aes.Decrypt(encrypted);
            return decrypted.Length <= 4 ? "****" : "****" + decrypted[^4..];
        }
        catch { return "***"; }
    }
}
