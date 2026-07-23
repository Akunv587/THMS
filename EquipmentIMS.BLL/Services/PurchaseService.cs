using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Models.Exceptions;

namespace EquipmentIMS.BLL.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IUnitOfWork _uow;
    private readonly IRepository<PurPurchaseOrder> _orderRepo;
    private readonly IRepository<PurPurchaseOrderDetail> _detailRepo;
    private readonly INumberGeneratorService _numberGen;
    private readonly INotificationService _notification;
    private readonly AppDbContext _db;

    public PurchaseService(IUnitOfWork uow, INumberGeneratorService numberGen,
        INotificationService notification, AppDbContext db)
    {
        _uow = uow;
        _orderRepo = uow.GetRepository<PurPurchaseOrder>();
        _detailRepo = uow.GetRepository<PurPurchaseOrderDetail>();
        _numberGen = numberGen;
        _notification = notification;
        _db = db;
    }

    public async Task<PagedResult<PurchaseOrderListDto>> GetListAsync(PurchaseOrderQueryDto query)
    {
        var q = _orderRepo.Query()
            .Include(o => o.Supplier)
            .Include(o => o.Warehouse)
            .Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(o => o.OrderNo.Contains(query.Keyword) || o.Supplier.Name.Contains(query.Keyword));
        if (query.SupplierId.HasValue)
            q = q.Where(o => o.SupplierId == query.SupplierId.Value);
        if (query.WarehouseId.HasValue)
            q = q.Where(o => o.WarehouseId == query.WarehouseId.Value);
        if (query.Status.HasValue)
            q = q.Where(o => o.Status == query.Status.Value);
        if (query.StartDate.HasValue)
            q = q.Where(o => o.OrderDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            q = q.Where(o => o.OrderDate <= query.EndDate.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return PagedResult<PurchaseOrderListDto>.Create(items.Select(ToListDto).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<PurchaseOrderDto> GetByIdAsync(long id)
    {
        var order = await _uow.PurchaseOrders.GetByIdWithDetailsAsync(id);
        if (order == null) throw new NotFoundException("采购订单不存在");
        return ToDto(order);
    }

    public async Task<long> CreateAsync(PurchaseOrderDto dto, string currentUser)
    {
        var orderNo = await _numberGen.GenerateAsync("PUR", "Pur_PurchaseOrder", "OrderNo");
        var order = new PurPurchaseOrder
        {
            OrderNo = orderNo,
            SupplierId = dto.SupplierId,
            WarehouseId = dto.WarehouseId,
            OrderDate = dto.OrderDate,
            ExpectedDate = dto.ExpectedDate,
            TotalAmount = dto.Details.Sum(d => d.Amount),
            Status = PurchaseOrderStatus.Draft,
            Remark = dto.Remark,
            CreateBy = currentUser,
            CreateTime = DateTime.Now
        };

        await _orderRepo.AddAsync(order);
        await _uow.SaveChangesAsync();

        if (dto.Details.Count > 0)
        {
            foreach (var d in dto.Details)
            {
                await _detailRepo.AddAsync(new PurPurchaseOrderDetail
                {
                    PurchaseOrderId = order.Id,
                    ProductId = d.ProductId,
                    Qty = d.Qty,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount,
                    Remark = d.Remark,
                    CreateBy = currentUser,
                    CreateTime = DateTime.Now
                });
            }
            await _uow.SaveChangesAsync();
        }

        return order.Id;
    }

    public async Task UpdateAsync(PurchaseOrderDto dto, string currentUser)
    {
        var order = await _uow.PurchaseOrders.GetByIdAsync(dto.Id)
            ?? throw new NotFoundException("采购订单不存在");
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("仅草稿状态可编辑");

        order.SupplierId = dto.SupplierId;
        order.WarehouseId = dto.WarehouseId;
        order.OrderDate = dto.OrderDate;
        order.ExpectedDate = dto.ExpectedDate;
        order.TotalAmount = dto.Details.Sum(d => d.Amount);
        order.Remark = dto.Remark;
        order.UpdateBy = currentUser;
        order.UpdateTime = DateTime.Now;

        _uow.PurchaseOrders.Update(order);

        // 删除旧明细
        var oldDetails = await _detailRepo.Query()
            .Where(d => d.PurchaseOrderId == dto.Id)
            .ToListAsync();
        foreach (var d in oldDetails)
            _detailRepo.Remove(d);

        // 插入新明细
        foreach (var d in dto.Details)
        {
            await _detailRepo.AddAsync(new PurPurchaseOrderDetail
            {
                PurchaseOrderId = dto.Id,
                ProductId = d.ProductId,
                Qty = d.Qty,
                UnitPrice = d.UnitPrice,
                Amount = d.Amount,
                Remark = d.Remark,
                CreateBy = currentUser,
                CreateTime = DateTime.Now
            });
        }

        await _uow.SaveChangesAsync();
    }

    public async Task SubmitAsync(long orderId, string currentUser)
    {
        var order = await _uow.PurchaseOrders.GetByIdWithDetailsAsync(orderId)
            ?? throw new NotFoundException("采购订单不存在");
        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("仅草稿状态可提交审批");
        if (order.SupplierId == 0) throw new InvalidOperationException("供应商必选");
        if (order.WarehouseId == 0) throw new InvalidOperationException("仓库必选");
        if (order.Details.Count == 0) throw new InvalidOperationException("必须有明细行");
        if (order.TotalAmount <= 0) throw new InvalidOperationException("金额必须大于0");

        order.Status = PurchaseOrderStatus.PendingApproval;
        order.UpdateBy = currentUser;
        order.UpdateTime = DateTime.Now;
        _uow.PurchaseOrders.Update(order);
        await _uow.SaveChangesAsync();
    }

    public async Task ApproveAsync(long orderId, long approverId, string approverName)
    {
        var order = await _uow.PurchaseOrders.GetByIdWithDetailsAsync(orderId)
            ?? throw new NotFoundException("采购订单不存在");
        if (order.Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException("仅待审批状态可审批");

        order.Status = PurchaseOrderStatus.Approved;
        order.Approver = approverName;
        order.ApproveTime = DateTime.Now;
        order.UpdateBy = approverName;
        order.UpdateTime = DateTime.Now;
        _uow.PurchaseOrders.Update(order);
        await _uow.SaveChangesAsync();

        var creatorUserId = await GetUserIdByNameAsync(order.CreateBy);
        await _notification.SendAsync(creatorUserId,
            "采购订单已审批", $"订单 {order.OrderNo} 已通过审批",
            NotificationType.ApprovalReminder, "PurchaseOrder", orderId);
    }

    public async Task RejectAsync(long orderId, long approverId, string approverName, string reason)
    {
        var order = await _uow.PurchaseOrders.GetByIdWithDetailsAsync(orderId)
            ?? throw new NotFoundException("采购订单不存在");
        if (order.Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException("仅待审批状态可驳回");

        order.Status = PurchaseOrderStatus.Draft;
        order.Approver = null;
        order.ApproveTime = null;
        order.Remark = string.IsNullOrEmpty(order.Remark) ? $"驳回原因: {reason}" : $"{order.Remark}\n驳回原因: {reason}";
        order.UpdateBy = approverName;
        order.UpdateTime = DateTime.Now;
        _uow.PurchaseOrders.Update(order);
        await _uow.SaveChangesAsync();

        var creatorUserId = await GetUserIdByNameAsync(order.CreateBy);
        await _notification.SendAsync(creatorUserId,
            "采购订单已驳回", $"订单 {order.OrderNo} 已驳回，原因: {reason}",
            NotificationType.ApprovalReminder, "PurchaseOrder", orderId);
    }

    public async Task CancelAsync(long orderId, string currentUser)
    {
        var order = await _uow.PurchaseOrders.GetByIdAsync(orderId)
            ?? throw new NotFoundException("采购订单不存在");
        if (order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.PendingApproval)
            throw new InvalidOperationException("仅草稿或待审批状态可取消");

        order.Status = PurchaseOrderStatus.Cancelled;
        order.UpdateBy = currentUser;
        order.UpdateTime = DateTime.Now;
        _uow.PurchaseOrders.Update(order);
        await _uow.SaveChangesAsync();
    }

    // ==================== 采购入库 ====================

    public async Task<PagedResult<PurchaseInListDto>> GetInListAsync(PurchaseInQueryDto query)
    {
        var q = _uow.GetRepository<PurPurchaseIn>().Query()
            .Include(i => i.PurchaseOrder)
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Where(i => !i.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(i => i.InNo.Contains(query.Keyword) || i.PurchaseOrder.OrderNo.Contains(query.Keyword));
        if (query.SupplierId.HasValue)
            q = q.Where(i => i.SupplierId == query.SupplierId.Value);
        if (query.WarehouseId.HasValue)
            q = q.Where(i => i.WarehouseId == query.WarehouseId.Value);
        if (query.StartDate.HasValue)
            q = q.Where(i => i.InDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            q = q.Where(i => i.InDate <= query.EndDate.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(i => i.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return PagedResult<PurchaseInListDto>.Create(items.Select(i => new PurchaseInListDto
            {
                Id = i.Id,
                InNo = i.InNo,
                PurchaseOrderId = i.PurchaseOrderId,
                OrderNo = i.PurchaseOrder?.OrderNo ?? "",
                SupplierId = i.SupplierId,
                SupplierName = i.Supplier?.Name ?? "",
                WarehouseId = i.WarehouseId,
                WarehouseName = i.Warehouse?.Name ?? "",
                InDate = i.InDate,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                Operator = i.Operator ?? "",
                CreateTime = i.CreateTime
            }).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<long> PurchaseInAsync(PurchaseInRequestDto dto, string currentUser)
    {
        long purchaseInId = 0;
        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var order = await _uow.PurchaseOrders.GetByIdAsync(dto.PurchaseOrderId)
                    ?? throw new NotFoundException("采购订单不存在");

                if (order.Status != PurchaseOrderStatus.Approved && order.Status != PurchaseOrderStatus.PartialReceived)
                    throw new BusinessException("采购订单状态不允许入库", "INVALID_STATUS");

                // 加载订单明细
                var orderDetail = await _uow.GetRepository<PurPurchaseOrderDetail>().Query()
                    .Where(d => d.PurchaseOrderId == dto.PurchaseOrderId)
                    .Include(d => d.Product)
                    .ToListAsync();

                var inNo = await _numberGen.GenerateAsync("PIN", "Pur_PurchaseIn", "InNo");
                var purchaseIn = new PurPurchaseIn
                {
                    InNo = inNo,
                    PurchaseOrderId = dto.PurchaseOrderId,
                    SupplierId = order.SupplierId,
                    WarehouseId = order.WarehouseId,
                    InDate = dto.InDate,
                    TotalAmount = dto.Details.Sum(d => d.Amount),
                    Status = PurchaseInStatus.Received,
                    Operator = currentUser,
                    Remark = dto.Remark,
                    CreateBy = currentUser,
                    CreateTime = DateTime.Now
                };
                await _uow.GetRepository<PurPurchaseIn>().AddAsync(purchaseIn);
                await _uow.SaveChangesAsync();

                foreach (var item in dto.Details)
                {
                    var od = orderDetail.First(d => d.ProductId == item.ProductId);
                    var remainingQty = od.Qty - od.ReceivedQty;
                    if (item.Qty > remainingQty)
                        throw new BusinessException($"设备[{item.ProductCode}]入库数量超过订单剩余数量", "QTY_EXCEED");

                    // 查找或创建库存记录
                    var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(item.ProductId, order.WarehouseId);
                    if (stock == null)
                    {
                        stock = new BaseProductStock
                        {
                            ProductId = item.ProductId,
                            WarehouseId = order.WarehouseId,
                            Quantity = 0,
                            LockQty = 0,
                            CreateBy = currentUser,
                            CreateTime = DateTime.Now
                        };
                        await _uow.GetRepository<BaseProductStock>().AddAsync(stock);
                        await _uow.SaveChangesAsync();
                    }

                    var balanceBefore = stock.Quantity;
                    stock.Quantity += item.Qty;
                    if (stock.Quantity < 0) throw new BusinessException("库存数量异常", "STOCK_NEGATIVE");
                    stock.UpdateBy = currentUser;
                    stock.UpdateTime = DateTime.Now;

                    // 记录库存流水
                    await _uow.StockTransactions.AddAsync(new InvStockTransaction
                    {
                        TransactionNo = await _numberGen.GenerateWithTimeAsync("TXN", "Inv_StockTransaction", "TransactionNo"),
                        TransactionType = TransactionType.PurchaseIn,
                        ProductId = item.ProductId,
                        WarehouseId = order.WarehouseId,
                        Qty = item.Qty,
                        BalanceQty = balanceBefore + item.Qty,
                        RefType = "PurchaseIn",
                        RefId = purchaseIn.Id,
                        RefNo = inNo,
                        Operator = currentUser,
                        CreateBy = currentUser,
                        CreateTime = DateTime.Now
                    });

                    // 入库明细
                    await _uow.GetRepository<PurPurchaseInDetail>().AddAsync(new PurPurchaseInDetail
                    {
                        PurchaseInId = purchaseIn.Id,
                        ProductId = item.ProductId,
                        Qty = item.Qty,
                        UnitPrice = item.UnitPrice,
                        Amount = item.Amount,
                        CreateBy = currentUser,
                        CreateTime = DateTime.Now
                    });

                    // 更新订单明细已入库数量
                    od.ReceivedQty += item.Qty;
                }

                // 更新订单状态
                var allReceived = orderDetail.All(d => d.ReceivedQty >= d.Qty);
                order.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartialReceived;
                order.ReceivedAmount += dto.Details.Sum(d => d.Amount);
                _uow.PurchaseOrders.Update(order);

                await _uow.SaveChangesAsync();
                purchaseInId = purchaseIn.Id;
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("库存数据已被他人修改，请刷新后重试", "STOCK_CONFLICT");
        }

        return purchaseInId;
    }

    // ==================== 采购退货 ====================

    public async Task<PagedResult<PurchaseReturnListDto>> GetReturnListAsync(PurchaseReturnQueryDto query)
    {
        var q = _uow.GetRepository<PurPurchaseReturn>().Query()
            .Include(r => r.PurchaseIn)
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(r => r.ReturnNo.Contains(query.Keyword) || r.PurchaseIn.InNo.Contains(query.Keyword));
        if (query.SupplierId.HasValue)
            q = q.Where(r => r.SupplierId == query.SupplierId.Value);
        if (query.WarehouseId.HasValue)
            q = q.Where(r => r.WarehouseId == query.WarehouseId.Value);
        if (query.StartDate.HasValue)
            q = q.Where(r => r.ReturnDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            q = q.Where(r => r.ReturnDate <= query.EndDate.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CreateTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return PagedResult<PurchaseReturnListDto>.Create(items.Select(r => new PurchaseReturnListDto
            {
                Id = r.Id,
                ReturnNo = r.ReturnNo,
                PurchaseInId = r.PurchaseInId,
                InNo = r.PurchaseIn?.InNo ?? "",
                SupplierId = r.SupplierId,
                SupplierName = r.Supplier?.Name ?? "",
                WarehouseId = r.WarehouseId,
                WarehouseName = r.Warehouse?.Name ?? "",
                ReturnDate = r.ReturnDate,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                Reason = r.Reason,
                Operator = r.Operator,
                CreateTime = r.CreateTime
            }).ToList(), total, query.Page, query.PageSize);
    }

    public async Task<long> PurchaseReturnAsync(PurchaseReturnRequestDto dto, string currentUser)
    {
        long purchaseReturnId = 0;
        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var purchaseIn = await _uow.GetRepository<PurPurchaseIn>().Query()
                    .Include(i => i.Details).ThenInclude(d => d.Product)
                    .Include(i => i.Supplier)
                    .Include(i => i.Warehouse)
                    .FirstOrDefaultAsync(i => i.Id == dto.PurchaseInId)
                    ?? throw new NotFoundException("入库单不存在");

                if (purchaseIn.Status != PurchaseInStatus.Received && purchaseIn.Status != PurchaseInStatus.Returned)
                    throw new BusinessException("入库单状态不允许退货", "INVALID_STATUS");

                var inDetails = purchaseIn.Details.ToList();

                var returnNo = await _numberGen.GenerateAsync("PRT", "Pur_PurchaseReturn", "ReturnNo");
                var purchaseReturn = new PurPurchaseReturn
                {
                    ReturnNo = returnNo,
                    PurchaseInId = dto.PurchaseInId,
                    SupplierId = purchaseIn.SupplierId,
                    WarehouseId = purchaseIn.WarehouseId,
                    ReturnDate = dto.ReturnDate,
                    TotalAmount = dto.Details.Sum(d => d.Amount),
                    Reason = dto.Reason,
                    Status = PurchaseReturnStatus.Returned,
                    Operator = currentUser,
                    Remark = dto.Remark,
                    CreateBy = currentUser,
                    CreateTime = DateTime.Now
                };
                await _uow.GetRepository<PurPurchaseReturn>().AddAsync(purchaseReturn);
                await _uow.SaveChangesAsync();

                foreach (var item in dto.Details)
                {
                    var ind = inDetails.First(d => d.ProductId == item.ProductId);
                    var remainingReturn = ind.Qty - ind.ReturnedQty;
                    if (item.Qty > remainingReturn)
                        throw new BusinessException($"设备[{item.ProductCode}]退货数量超过入库数量", "QTY_EXCEED");

                    var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(item.ProductId, purchaseIn.WarehouseId)
                        ?? throw new BusinessException($"设备[{item.ProductCode}]在仓库无库存", "STOCK_NOT_FOUND");

                    var balanceBefore = stock.Quantity;
                    stock.Quantity -= item.Qty;
                    if (stock.Quantity < 0) throw new BusinessException("库存数量异常", "STOCK_NEGATIVE");
                    stock.UpdateBy = currentUser;
                    stock.UpdateTime = DateTime.Now;

                    await _uow.StockTransactions.AddAsync(new InvStockTransaction
                    {
                        TransactionNo = await _numberGen.GenerateWithTimeAsync("TXN", "Inv_StockTransaction", "TransactionNo"),
                        TransactionType = TransactionType.PurchaseReturnOut,
                        ProductId = item.ProductId,
                        WarehouseId = purchaseIn.WarehouseId,
                        Qty = -item.Qty,
                        BalanceQty = balanceBefore - item.Qty,
                        RefType = "PurchaseReturn",
                        RefId = purchaseReturn.Id,
                        RefNo = returnNo,
                        Operator = currentUser,
                        CreateBy = currentUser,
                        CreateTime = DateTime.Now
                    });

                    await _uow.GetRepository<PurPurchaseReturnDetail>().AddAsync(new PurPurchaseReturnDetail
                    {
                        PurchaseReturnId = purchaseReturn.Id,
                        ProductId = item.ProductId,
                        Qty = item.Qty,
                        UnitPrice = item.UnitPrice,
                        Amount = item.Amount,
                        CreateBy = currentUser,
                        CreateTime = DateTime.Now
                    });

                    ind.ReturnedQty += item.Qty;
                }

                var allReturned = inDetails.All(d => d.ReturnedQty >= d.Qty);
                purchaseIn.Status = allReturned ? PurchaseInStatus.Returned : PurchaseInStatus.Received;
                purchaseIn.UpdateBy = currentUser;
                purchaseIn.UpdateTime = DateTime.Now;

                await _uow.SaveChangesAsync();
                purchaseReturnId = purchaseReturn.Id;
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("库存数据已被他人修改，请刷新后重试", "STOCK_CONFLICT");
        }

        return purchaseReturnId;
    }

    private async Task<long> GetUserIdByNameAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName)) return 0;
        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.UserName == userName);
        return user?.Id ?? 0;
    }

    private static PurchaseOrderListDto ToListDto(PurPurchaseOrder o) => new()
    {
        Id = o.Id,
        OrderNo = o.OrderNo,
        SupplierId = o.SupplierId,
        SupplierName = o.Supplier?.Name ?? "",
        WarehouseId = o.WarehouseId,
        WarehouseName = o.Warehouse?.Name ?? "",
        OrderDate = o.OrderDate,
        ExpectedDate = o.ExpectedDate,
        TotalAmount = o.TotalAmount,
        Status = o.Status,
        CreateBy = o.CreateBy,
        CreateTime = o.CreateTime
    };

    private static PurchaseOrderDto ToDto(PurPurchaseOrder o) => new()
    {
        Id = o.Id,
        OrderNo = o.OrderNo,
        SupplierId = o.SupplierId,
        SupplierName = o.Supplier?.Name ?? "",
        WarehouseId = o.WarehouseId,
        WarehouseName = o.Warehouse?.Name ?? "",
        OrderDate = o.OrderDate,
        ExpectedDate = o.ExpectedDate,
        TotalAmount = o.TotalAmount,
        ReceivedAmount = o.ReceivedAmount,
        Status = o.Status,
        Approver = o.Approver,
        ApproveTime = o.ApproveTime,
        Remark = o.Remark,
        CreateBy = o.CreateBy,
        CreateTime = o.CreateTime,
        Details = o.Details.Select(d => new PurchaseOrderDetailDto
        {
            Id = d.Id,
            PurchaseOrderId = d.PurchaseOrderId,
            ProductId = d.ProductId,
            ProductCode = d.Product?.Code ?? "",
            ProductName = d.Product?.Name ?? "",
            Spec = d.Product?.Spec,
            Unit = d.Product?.Unit,
            Qty = d.Qty,
            UnitPrice = d.UnitPrice,
            Amount = d.Amount,
            ReceivedQty = d.ReceivedQty,
            Remark = d.Remark
        }).ToList()
    };
}
