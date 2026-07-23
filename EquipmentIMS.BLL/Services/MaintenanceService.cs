using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.BLL.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IUnitOfWork _uow;
    private readonly INumberGeneratorService _numberGen;

    public MaintenanceService(IUnitOfWork uow, INumberGeneratorService numberGen)
    {
        _uow = uow;
        _numberGen = numberGen;
    }

    public async Task<PagedResult<RepairOrderListDto>> GetListAsync(long? technicianId, int? status, long? deviceArchiveId, long currentUserId, bool isManager, int page, int pageSize)
    {
        var query = _uow.GetRepository<MaintRepairOrder>().Query()
            .Include(e => e.DeviceArchive)
            .Include(e => e.Customer)
            .Where(e => !e.IsDeleted);

        if (!isManager)
            query = query.Where(e => e.TechnicianId == currentUserId);

        if (technicianId.HasValue)
            query = query.Where(e => e.TechnicianId == technicianId.Value);
        if (status.HasValue)
            query = query.Where(e => (int)e.Status == status.Value);
        if (deviceArchiveId.HasValue)
            query = query.Where(e => e.DeviceArchiveId == deviceArchiveId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.CreateTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new RepairOrderListDto
            {
                Id = e.Id,
                OrderNo = e.OrderNo,
                SerialNo = e.DeviceArchive.SerialNo,
                ProductName = e.DeviceArchive.Product.Name,
                CustomerName = e.Customer.Name,
                FaultDesc = e.FaultDesc,
                FaultType = e.FaultType,
                Status = (int)e.Status,
                StatusName = GetStatusName(e.Status),
                TechnicianName = e.TechnicianName,
                AssignTime = e.AssignTime,
                CompleteTime = e.CompleteTime,
                RepairCost = e.RepairCost,
                PartsCost = e.PartsCost,
                TotalCost = e.TotalCost,
                IsWarranty = e.IsWarranty,
                CreateTime = e.CreateTime
            }).ToListAsync();

        return PagedResult<RepairOrderListDto>.Create(items, total, page, pageSize);
    }

    public async Task<RepairOrderDto?> GetByIdAsync(long id)
    {
        var e = await _uow.GetRepository<MaintRepairOrder>().Query()
            .Include(x => x.DeviceArchive).ThenInclude(x => x.Product)
            .Include(x => x.Customer)
            .Include(x => x.RepairParts).ThenInclude(x => x.Product)
            .Include(x => x.RepairParts).ThenInclude(x => x.Warehouse)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e == null ? null : MapToDto(e);
    }

    public async Task<long> CreateAsync(RepairOrderDto dto, string currentUser)
    {
        var archive = await _uow.GetRepository<MaintDeviceArchive>().Query()
            .FirstOrDefaultAsync(x => x.Id == dto.DeviceArchiveId && !x.IsDeleted)
            ?? throw new InvalidOperationException("设备档案不存在");

        var orderNo = await _numberGen.GenerateAsync("RP", "Maint_RepairOrder", "OrderNo");
        var entity = new MaintRepairOrder
        {
            OrderNo = orderNo,
            DeviceArchiveId = dto.DeviceArchiveId,
            CustomerId = archive.CustomerId,
            FaultDesc = dto.FaultDesc,
            FaultType = dto.FaultType,
            Status = RepairOrderStatus.PendingDispatch,
            IsWarranty = archive.WarrantyEnd.HasValue && archive.WarrantyEnd.Value >= DateTime.Today,
            CreateBy = currentUser
        };
        await _uow.GetRepository<MaintRepairOrder>().AddAsync(entity);
        return entity.Id;
    }

    // ========== 工单状态流转 ==========

    public async Task DispatchAsync(long orderId, long technicianId, string dispatcher)
    {
        var order = await GetOrderAsync(orderId);
        EnsureStatus(order, RepairOrderStatus.PendingDispatch, "只有待派工的工单才能派工");

        var tech = await _uow.GetRepository<SysUser>().GetByIdAsync(technicianId)
            ?? throw new InvalidOperationException("技术员不存在");

        SetArchiveStatus(order, DeviceStatus.InRepair, dispatcher);
        order.Status = RepairOrderStatus.Dispatched;
        order.TechnicianId = technicianId;
        order.TechnicianName = tech.RealName;
        order.AssignTime = DateTime.Now;
        await SaveTransitionAsync(order, dispatcher);
    }

    public async Task StartRepairAsync(long orderId, string technician)
    {
        var order = await GetOrderAsync(orderId);
        EnsureStatus(order, RepairOrderStatus.Dispatched, "只有已派工的工单才能开始维修");
        order.Status = RepairOrderStatus.InRepair;
        order.StartTime = DateTime.Now;
        await SaveTransitionAsync(order, technician);
    }

    public async Task CompleteRepairAsync(long orderId, string repairDesc, decimal workHours, decimal repairCost, string technician)
    {
        var order = await GetOrderAsync(orderId);
        EnsureStatus(order, RepairOrderStatus.InRepair, "只有维修中的工单才能完成维修");
        order.Status = RepairOrderStatus.PendingConfirm;
        order.RepairDesc = repairDesc;
        order.WorkHours = workHours;
        order.RepairCost = repairCost;
        order.TotalCost = repairCost + order.PartsCost;
        order.CompleteTime = DateTime.Now;
        await SaveTransitionAsync(order, technician);
    }

    public async Task ConfirmAsync(long orderId, string confirmer)
    {
        var order = await GetOrderAsync(orderId);
        EnsureStatus(order, RepairOrderStatus.PendingConfirm, "只有待确认的工单才能确认完工");
        order.Status = RepairOrderStatus.Completed;
        await SaveTransitionAsync(order, confirmer);
    }

    public async Task CloseAsync(long orderId, string user)
    {
        var order = await GetOrderAsync(orderId);
        EnsureStatus(order, RepairOrderStatus.Completed, "只有已完工的工单才能关闭归档");
        SetArchiveStatus(order, DeviceStatus.Normal, user);
        order.Status = RepairOrderStatus.Closed;
        await SaveTransitionAsync(order, user);
    }

    public async Task CancelAsync(long orderId, string user)
    {
        var order = await GetOrderAsync(orderId);
        if (order.Status == RepairOrderStatus.Closed)
            throw new InvalidOperationException("已关闭的工单不能取消");
        SetArchiveStatus(order, DeviceStatus.Normal, user);
        order.Status = RepairOrderStatus.Cancelled;
        await SaveTransitionAsync(order, user);
    }

    private async Task<MaintRepairOrder> GetOrderAsync(long orderId) =>
        await _uow.GetRepository<MaintRepairOrder>().GetByIdAsync(orderId)
            ?? throw new InvalidOperationException("工单不存在");

    private static void EnsureStatus(MaintRepairOrder order, RepairOrderStatus expected, string message)
    {
        if (order.Status != expected) throw new InvalidOperationException(message);
    }

    private async void SetArchiveStatus(MaintRepairOrder order, DeviceStatus targetStatus, string user)
    {
        var archive = await _uow.GetRepository<MaintDeviceArchive>().GetByIdAsync(order.DeviceArchiveId);
        if (archive != null && archive.Status != targetStatus)
        {
            archive.Status = targetStatus;
            archive.UpdateTime = DateTime.Now;
            archive.UpdateBy = user;
            _uow.GetRepository<MaintDeviceArchive>().Update(archive);
        }
    }

    private async Task SaveTransitionAsync(MaintRepairOrder order, string user)
    {
        order.UpdateBy = user;
        order.UpdateTime = DateTime.Now;
        _uow.GetRepository<MaintRepairOrder>().Update(order);
        await _uow.SaveChangesAsync();
    }

    // ========== 维修配件 ==========

    public async Task RecordRepairPartAsync(long repairOrderId, List<RepairPartDto> parts, string currentUser)
    {
        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var order = await _uow.GetRepository<MaintRepairOrder>().GetByIdAsync(repairOrderId)
                    ?? throw new InvalidOperationException("工单不存在");

                decimal totalPartsCost = 0m;

                foreach (var part in parts)
                {
                    if (part.Qty <= 0)
                        throw new InvalidOperationException($"配件 {part.ProductName} 数量必须大于0");

                    var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(part.ProductId, part.WarehouseId)
                        ?? throw new InvalidOperationException($"配件 [{part.ProductName}] 在所选仓库中无库存");

                    var available = stock.Quantity - stock.LockQty;
                    if (available < part.Qty)
                        throw new InvalidOperationException($"配件 [{part.ProductName}] 可用库存不足（可用:{available}, 需要:{part.Qty}）");

                    stock.Quantity -= part.Qty;
                    if (stock.Quantity < 0) stock.Quantity = 0;
                    stock.UpdateTime = DateTime.Now;
                    stock.UpdateBy = currentUser;
                    _uow.GetRepository<BaseProductStock>().Update(stock);

                    var txn = new InvStockTransaction
                    {
                        TransactionNo = await _numberGen.GenerateAsync("ST", "Inv_StockTransaction", "TransactionNo"),
                        TransactionType = TransactionType.RepairUse,
                        ProductId = part.ProductId,
                        WarehouseId = part.WarehouseId,
                        Qty = -part.Qty,
                        BalanceQty = stock.Quantity,
                        RefType = "RepairOrder",
                        RefId = repairOrderId,
                        RefNo = order.OrderNo,
                        Operator = currentUser,
                        CreateTime = DateTime.Now,
                        CreateBy = currentUser
                    };
                    await _uow.StockTransactions.AddAsync(txn);

                    var repairPart = new MaintRepairPart
                    {
                        RepairOrderId = repairOrderId,
                        ProductId = part.ProductId,
                        WarehouseId = part.WarehouseId,
                        Qty = part.Qty,
                        UnitPrice = part.UnitPrice,
                        Amount = part.Qty * part.UnitPrice,
                        CreateBy = currentUser
                    };
                    await _uow.GetRepository<MaintRepairPart>().AddAsync(repairPart);

                    totalPartsCost += repairPart.Amount;
                }

                order.PartsCost += totalPartsCost;
                order.TotalCost = order.RepairCost + order.PartsCost;
                order.UpdateBy = currentUser;
                order.UpdateTime = DateTime.Now;
                _uow.GetRepository<MaintRepairOrder>().Update(order);

                await _uow.SaveChangesAsync();
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("库存数据已被修改，请刷新后重试");
        }
    }

    public async Task<List<RepairPartDto>> GetRepairPartsAsync(long repairOrderId)
    {
        var parts = await _uow.GetRepository<MaintRepairPart>().Query()
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Where(x => x.RepairOrderId == repairOrderId && !x.IsDeleted)
            .ToListAsync();

        return parts.Select(p => new RepairPartDto
        {
            Id = p.Id,
            RepairOrderId = p.RepairOrderId,
            ProductId = p.ProductId,
            ProductCode = p.Product?.Code,
            ProductName = p.Product?.Name,
            WarehouseId = p.WarehouseId,
            Qty = p.Qty,
            UnitPrice = p.UnitPrice,
            Amount = p.Amount,
            Remark = p.Remark
        }).ToList();
    }

    // ========== 辅助映射 ==========

    private static string GetStatusName(RepairOrderStatus s) => s switch
    {
        RepairOrderStatus.PendingDispatch => "待派工",
        RepairOrderStatus.Dispatched => "已派工",
        RepairOrderStatus.InRepair => "维修中",
        RepairOrderStatus.PendingConfirm => "待确认",
        RepairOrderStatus.Completed => "已完工",
        RepairOrderStatus.Closed => "已关闭",
        RepairOrderStatus.Cancelled => "已取消",
        _ => ""
    };

    private static RepairOrderDto MapToDto(MaintRepairOrder e) => new()
    {
        Id = e.Id,
        OrderNo = e.OrderNo,
        DeviceArchiveId = e.DeviceArchiveId,
        SerialNo = e.DeviceArchive?.SerialNo,
        CustomerId = e.CustomerId,
        CustomerName = e.Customer?.Name,
        FaultDesc = e.FaultDesc,
        FaultType = e.FaultType,
        Status = (int)e.Status,
        StatusName = GetStatusName(e.Status),
        TechnicianId = e.TechnicianId,
        TechnicianName = e.TechnicianName,
        AssignTime = e.AssignTime,
        StartTime = e.StartTime,
        CompleteTime = e.CompleteTime,
        RepairDesc = e.RepairDesc,
        WorkHours = e.WorkHours,
        RepairCost = e.RepairCost,
        PartsCost = e.PartsCost,
        TotalCost = e.TotalCost,
        IsWarranty = e.IsWarranty,
        Remark = e.Remark,
        CreateTime = e.CreateTime,
        Parts = e.RepairParts.Select(p => new RepairPartDto
        {
            Id = p.Id,
            RepairOrderId = p.RepairOrderId,
            ProductId = p.ProductId,
            ProductCode = p.Product?.Code,
            ProductName = p.Product?.Name,
            WarehouseId = p.WarehouseId,
            Qty = p.Qty,
            UnitPrice = p.UnitPrice,
            Amount = p.Amount,
            Remark = p.Remark
        }).ToList()
    };

    // ========== 保养计划 ==========

    public async Task<PagedResult<MaintenancePlanListDto>> GetPlansAsync(long? deviceArchiveId, int page, int pageSize)
    {
        var query = _uow.GetRepository<MaintMaintenancePlan>().Query()
            .Include(e => e.DeviceArchive).ThenInclude(e => e.Product)
            .Include(e => e.DeviceArchive).ThenInclude(e => e.Customer)
            .Where(e => !e.IsDeleted);

        if (deviceArchiveId.HasValue)
            query = query.Where(e => e.DeviceArchiveId == deviceArchiveId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.NextDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new MaintenancePlanListDto
            {
                Id = e.Id,
                DeviceArchiveId = e.DeviceArchiveId,
                SerialNo = e.DeviceArchive.SerialNo,
                ProductName = e.DeviceArchive.Product.Name,
                CustomerName = e.DeviceArchive.Customer.Name,
                PlanName = e.PlanName,
                CycleType = (int)e.CycleType,
                CycleTypeName = GetCycleTypeName(e.CycleType),
                CycleDays = e.CycleDays,
                NextDate = e.NextDate,
                Content = e.Content,
                IsActive = e.IsActive,
                Remark = e.Remark,
                CreateTime = e.CreateTime
            }).ToListAsync();

        return PagedResult<MaintenancePlanListDto>.Create(items, total, page, pageSize);
    }

    public async Task<List<MaintenancePlanDto>> GetPlansByDeviceAsync(long deviceId)
    {
        var plans = await _uow.GetRepository<MaintMaintenancePlan>().Query()
            .Include(e => e.DeviceArchive)
            .Where(e => e.DeviceArchiveId == deviceId && !e.IsDeleted)
            .OrderBy(e => e.NextDate)
            .ToListAsync();

        return plans.Select(MapPlanToDto).ToList();
    }

    public async Task<MaintenancePlanDto?> GetPlanByIdAsync(long id)
    {
        var e = await _uow.GetRepository<MaintMaintenancePlan>().Query()
            .Include(x => x.DeviceArchive)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e == null ? null : MapPlanToDto(e);
    }

    public async Task<long> CreatePlanAsync(MaintenancePlanDto dto, string currentUser)
    {
        var entity = new MaintMaintenancePlan
        {
            DeviceArchiveId = dto.DeviceArchiveId,
            PlanName = dto.PlanName,
            CycleType = (CycleType)dto.CycleType,
            CycleDays = dto.CycleDays,
            NextDate = dto.NextDate,
            Content = dto.Content,
            IsActive = dto.IsActive,
            Remark = dto.Remark,
            CreateBy = currentUser
        };
        await _uow.GetRepository<MaintMaintenancePlan>().AddAsync(entity);
        return entity.Id;
    }

    public async Task UpdatePlanAsync(MaintenancePlanDto dto, string currentUser)
    {
        var entity = await _uow.GetRepository<MaintMaintenancePlan>().GetByIdAsync(dto.Id)
            ?? throw new InvalidOperationException("保养计划不存在");

        entity.PlanName = dto.PlanName;
        entity.CycleType = (CycleType)dto.CycleType;
        entity.CycleDays = dto.CycleDays;
        entity.NextDate = dto.NextDate;
        entity.Content = dto.Content;
        entity.IsActive = dto.IsActive;
        entity.Remark = dto.Remark;
        entity.UpdateBy = currentUser;
        entity.UpdateTime = DateTime.Now;
        _uow.GetRepository<MaintMaintenancePlan>().Update(entity);
        await _uow.SaveChangesAsync();
    }

    public async Task DeletePlanAsync(long id)
    {
        var entity = await _uow.GetRepository<MaintMaintenancePlan>().GetByIdAsync(id)
            ?? throw new InvalidOperationException("保养计划不存在");
        entity.IsDeleted = true;
        entity.UpdateTime = DateTime.Now;
        _uow.GetRepository<MaintMaintenancePlan>().Update(entity);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<MaintenancePlanDto>> GetDuePlansAsync()
    {
        var today = DateTime.Today;
        var plans = await _uow.GetRepository<MaintMaintenancePlan>().Query()
            .Include(e => e.DeviceArchive)
            .Where(e => !e.IsDeleted && e.IsActive && e.NextDate <= today)
            .OrderBy(e => e.NextDate)
            .ToListAsync();

        return plans.Select(MapPlanToDto).ToList();
    }

    private static string GetCycleTypeName(CycleType t) => t switch
    {
        CycleType.Monthly => "每月",
        CycleType.Quarterly => "每季",
        CycleType.SemiAnnual => "每半年",
        CycleType.Annual => "每年",
        _ => ""
    };

    private static MaintenancePlanDto MapPlanToDto(MaintMaintenancePlan e) => new()
    {
        Id = e.Id,
        DeviceArchiveId = e.DeviceArchiveId,
        SerialNo = e.DeviceArchive?.SerialNo,
        PlanName = e.PlanName,
        CycleType = (int)e.CycleType,
        CycleDays = e.CycleDays,
        NextDate = e.NextDate,
        Content = e.Content,
        IsActive = e.IsActive,
        Remark = e.Remark,
        CreateTime = e.CreateTime
    };
}
