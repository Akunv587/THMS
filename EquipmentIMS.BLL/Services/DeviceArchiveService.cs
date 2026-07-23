using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Services;

public class DeviceArchiveService : IDeviceArchiveService
{
    private readonly IRepository<MaintDeviceArchive> _repo;
    private readonly IRepository<MaintRepairOrder> _repairRepo;

    public DeviceArchiveService(IUnitOfWork uow)
    {
        _repo = uow.GetRepository<MaintDeviceArchive>();
        _repairRepo = uow.GetRepository<MaintRepairOrder>();
    }

    public async Task<PagedResult<DeviceArchiveListDto>> GetListAsync(string? serialNo, long? customerId, int? status, int page, int pageSize)
    {
        System.Linq.Expressions.Expression<Func<MaintDeviceArchive, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(serialNo) || customerId.HasValue || status.HasValue)
        {
            predicate = e =>
                (string.IsNullOrWhiteSpace(serialNo) || e.SerialNo.Contains(serialNo))
                && (!customerId.HasValue || e.CustomerId == customerId.Value)
                && (!status.HasValue || (int)e.Status == status.Value);
        }

        var result = await _repo.GetPagedAsync(page, pageSize, predicate);

        return new PagedResult<DeviceArchiveListDto>
        {
            Items = result.Items.Select(MapToListDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<DeviceArchiveDto?> GetByIdAsync(long id)
    {
        var entity = await _repo.Query()
            .Include(e => e.Product)
            .Include(e => e.Customer)
            .Include(e => e.SaleOrder)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<DeviceArchiveDto?> FindBySerialNoAsync(string serialNo)
    {
        var entity = await _repo.Query()
            .Include(e => e.Product)
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.SerialNo == serialNo && !e.IsDeleted);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<long> CreateAsync(DeviceArchiveDto dto, string currentUser)
    {
        var entity = new MaintDeviceArchive
        {
            SerialNo = dto.SerialNo,
            ProductId = dto.ProductId,
            CustomerId = dto.CustomerId,
            SaleOrderId = dto.SaleOrderId,
            InstallDate = dto.InstallDate,
            WarrantyStart = dto.WarrantyStart,
            WarrantyEnd = dto.WarrantyEnd,
            Status = DeviceStatus.Normal,
            Remark = dto.Remark,
            CreateBy = currentUser
        };
        await _repo.AddAsync(entity);
        return entity.Id;
    }

    public async Task UpdateAsync(DeviceArchiveDto dto, string currentUser)
    {
        var entity = await _repo.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return;

        entity.SerialNo = dto.SerialNo;
        entity.ProductId = dto.ProductId;
        entity.CustomerId = dto.CustomerId;
        entity.SaleOrderId = dto.SaleOrderId;
        entity.InstallDate = dto.InstallDate;
        entity.WarrantyStart = dto.WarrantyStart;
        entity.WarrantyEnd = dto.WarrantyEnd;
        entity.Remark = dto.Remark;
        entity.UpdateBy = currentUser;
        entity.UpdateTime = DateTime.Now;
        _repo.Update(entity);
    }

    public Task DeleteAsync(long id)
    {
        _repo.Query().Where(e => e.Id == id && !e.IsDeleted).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsDeleted, true));
        return Task.CompletedTask;
    }

    public async Task<List<RepairOrderDto>> GetRepairOrdersAsync(long deviceArchiveId)
    {
        var list = await _repairRepo.Query()
            .Where(e => e.DeviceArchiveId == deviceArchiveId && !e.IsDeleted)
            .OrderByDescending(e => e.CreateTime)
            .ToListAsync();

        return list.Select(e => new RepairOrderDto
        {
            Id = e.Id,
            OrderNo = e.OrderNo,
            DeviceArchiveId = e.DeviceArchiveId,
            CustomerId = e.CustomerId,
            FaultDesc = e.FaultDesc,
            FaultType = e.FaultType,
            Status = (int)e.Status,
            StatusName = e.Status switch
            {
                RepairOrderStatus.PendingDispatch => "待派工",
                RepairOrderStatus.Dispatched => "已派工",
                RepairOrderStatus.InRepair => "维修中",
                RepairOrderStatus.PendingConfirm => "待确认",
                RepairOrderStatus.Completed => "已完成",
                RepairOrderStatus.Closed => "已关闭",
                RepairOrderStatus.Cancelled => "已取消",
                _ => ""
            },
            TechnicianName = e.TechnicianName,
            AssignTime = e.AssignTime,
            CompleteTime = e.CompleteTime,
            RepairDesc = e.RepairDesc,
            TotalCost = e.TotalCost,
            IsWarranty = e.IsWarranty,
            CreateTime = e.CreateTime
        }).ToList();
    }

    private DeviceArchiveListDto MapToListDto(MaintDeviceArchive e) => new()
    {
        Id = e.Id,
        SerialNo = e.SerialNo,
        ProductCode = e.Product?.Code,
        ProductName = e.Product?.Name,
        CustomerName = e.Customer?.Name,
        Status = (int)e.Status,
        StatusName = e.Status switch
        {
            DeviceStatus.Normal => "正常",
            DeviceStatus.InRepair => "维修中",
            DeviceStatus.Scrapped => "报废",
            _ => ""
        },
        WarrantyEnd = e.WarrantyEnd,
        InstallDate = e.InstallDate,
        CreateTime = e.CreateTime
    };

    private DeviceArchiveDto MapToDto(MaintDeviceArchive e) => new()
    {
        Id = e.Id,
        SerialNo = e.SerialNo,
        ProductId = e.ProductId,
        ProductCode = e.Product?.Code,
        ProductName = e.Product?.Name,
        CustomerId = e.CustomerId,
        CustomerName = e.Customer?.Name,
        SaleOrderId = e.SaleOrderId,
        InstallDate = e.InstallDate,
        WarrantyStart = e.WarrantyStart,
        WarrantyEnd = e.WarrantyEnd,
        Status = (int)e.Status,
        StatusName = e.Status switch
        {
            DeviceStatus.Normal => "正常",
            DeviceStatus.InRepair => "维修中",
            DeviceStatus.Scrapped => "报废",
            _ => ""
        },
        Remark = e.Remark,
        CreateTime = e.CreateTime
    };
}
