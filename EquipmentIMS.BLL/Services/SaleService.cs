using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;
using EquipmentIMS.Models.Enums;
using EquipmentIMS.Models.Exceptions;

namespace EquipmentIMS.BLL.Services;

public class SaleService : ISaleService
{
    private readonly IUnitOfWork _uow;
    private readonly IRepository<SaleSaleOrder> _orderRepo;
    private readonly IRepository<SaleSaleOrderDetail> _detailRepo;
    private readonly IRepository<BaseCustomer> _customerRepo;
    private readonly IRepository<BaseCustomerPrice> _customerPriceRepo;
    private readonly IRepository<BaseProduct> _productRepo;
    private readonly IRepository<SaleSaleOut> _outRepo;
    private readonly INumberGeneratorService _numberGen;
    private readonly ISystemConfigService _config;
    private readonly INotificationService _notification;
    private readonly IStockService _stockService;
    private readonly DAL.AppDbContext _db;

    public SaleService(IUnitOfWork uow, INumberGeneratorService numberGen,
        ISystemConfigService config, INotificationService notification,
        IStockService stockService, DAL.AppDbContext db)
    {
        _uow = uow;
        _orderRepo = uow.GetRepository<SaleSaleOrder>();
        _detailRepo = uow.GetRepository<SaleSaleOrderDetail>();
        _customerRepo = uow.GetRepository<BaseCustomer>();
        _customerPriceRepo = uow.GetRepository<BaseCustomerPrice>();
        _productRepo = uow.GetRepository<BaseProduct>();
        _outRepo = uow.GetRepository<SaleSaleOut>();
        _numberGen = numberGen;
        _config = config;
        _notification = notification;
        _stockService = stockService;
        _db = db;
    }

    public async Task<PagedResult<SaleOrderListDto>> GetListAsync(SaleOrderQueryDto query, string currentUser, bool isManager)
    {
        var q = _orderRepo.Query()
            .Include(o => o.Customer)
            .Where(o => !o.IsDeleted);

        if (!isManager)
            q = q.Where(o => o.CreateBy == currentUser);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(o => o.OrderNo.Contains(query.Keyword) || o.Customer.Name.Contains(query.Keyword));
        if (query.CustomerId.HasValue)
            q = q.Where(o => o.CustomerId == query.CustomerId.Value);
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

        return new PagedResult<SaleOrderListDto>
        {
            Items = items.Select(ToListDto).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<SaleOrderDto> GetByIdAsync(long id)
    {
        var order = await _uow.SaleOrders.GetByIdWithDetailsAsync(id);
        if (order == null) throw new NotFoundException("销售订单不存在");
        return ToDto(order);
    }

    public async Task<long> CreateAsync(SaleOrderDto dto, string currentUser)
    {
        await CheckCreditAsync(dto.CustomerId, dto.Details.Sum(d => d.Amount));

        var orderNo = await _numberGen.GenerateAsync("SALE", "Sale_SaleOrder", "OrderNo");
        var order = new SaleSaleOrder
        {
            OrderNo = orderNo,
            CustomerId = dto.CustomerId,
            OrderDate = dto.OrderDate,
            ExpectedDate = dto.ExpectedDate,
            TotalAmount = dto.Details.Sum(d => d.Amount),
            Status = SaleOrderStatus.Draft,
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
                await _detailRepo.AddAsync(new SaleSaleOrderDetail
                {
                    SaleOrderId = order.Id,
                    ProductId = d.ProductId,
                    WarehouseId = d.WarehouseId,
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

    public async Task UpdateAsync(SaleOrderDto dto, string currentUser)
    {
        var order = await _uow.SaleOrders.GetByIdAsync(dto.Id)
            ?? throw new NotFoundException("销售订单不存在");
        if (order.Status != SaleOrderStatus.Draft)
            throw new InvalidOperationException("仅草稿状态可编辑");

        var totalAmount = dto.Details.Sum(d => d.Amount);
        await CheckCreditAsync(dto.CustomerId, totalAmount);

        order.CustomerId = dto.CustomerId;
        order.OrderDate = dto.OrderDate;
        order.ExpectedDate = dto.ExpectedDate;
        order.TotalAmount = totalAmount;
        order.Remark = dto.Remark;
        order.UpdateBy = currentUser;
        order.UpdateTime = DateTime.Now;
        _uow.SaleOrders.Update(order);

        var oldDetails = await _detailRepo.Query()
            .Where(d => d.SaleOrderId == dto.Id)
            .ToListAsync();
        foreach (var d in oldDetails)
            _detailRepo.Remove(d);

        foreach (var d in dto.Details)
        {
            await _detailRepo.AddAsync(new SaleSaleOrderDetail
            {
                SaleOrderId = dto.Id,
                ProductId = d.ProductId,
                WarehouseId = d.WarehouseId,
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
        var order = await _uow.SaleOrders.GetByIdWithDetailsAsync(orderId)
            ?? throw new NotFoundException("销售订单不存在");
        if (order.Status != SaleOrderStatus.Draft)
            throw new InvalidOperationException("仅草稿状态可提交审批");
        if (order.CustomerId == 0) throw new InvalidOperationException("客户必选");
        if (order.Details.Count == 0) throw new InvalidOperationException("必须有明细行");
        if (order.TotalAmount <= 0) throw new InvalidOperationException("金额必须大于0");

        if (order.Details.Any(d => d.WarehouseId == 0))
            throw new InvalidOperationException("每行必须选择出库仓库");

        await CheckCreditAsync(order.CustomerId, order.TotalAmount);

        order.Status = SaleOrderStatus.PendingApproval;
        order.UpdateBy = currentUser;
        order.UpdateTime = DateTime.Now;
        _uow.SaleOrders.Update(order);
        await _uow.SaveChangesAsync();
    }

    public async Task ApproveAsync(long orderId, string approver)
    {
        await using var tx = await _uow.BeginTransactionAsync();
        try
        {
            var order = await _uow.SaleOrders.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException("销售订单不存在");
            if (order.Status != SaleOrderStatus.PendingApproval)
                throw new BusinessException("仅待审批状态可审批", "INVALID_STATUS");

            // 遍历明细校验并锁定库存
            foreach (var detail in order.Details)
            {
                var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(detail.ProductId, detail.WarehouseId);
                if (stock == null)
                {
                    var productName = detail.Product?.Name ?? detail.ProductId.ToString();
                    var warehouseName = detail.Warehouse?.Name ?? detail.WarehouseId.ToString();
                    throw new BusinessException($"设备 [{productName}] 在仓库 [{warehouseName}] 无库存记录", "STOCK_INSUFFICIENT");
                }

                var available = stock.Quantity - stock.LockQty;
                if (available < detail.Qty)
                {
                    var productName = detail.Product?.Name ?? detail.ProductId.ToString();
                    throw new BusinessException(
                        $"设备 [{productName}] 库存不足（可用: {available}, 需要: {detail.Qty}）",
                        "STOCK_INSUFFICIENT");
                }

                // 锁定库存
                stock.LockQty += detail.Qty;
            }

            order.Status = SaleOrderStatus.Approved;
            order.Approver = approver;
            order.ApproveTime = DateTime.Now;
            order.UpdateBy = approver;
            order.UpdateTime = DateTime.Now;
            _uow.SaleOrders.Update(order);

            await _uow.SaveChangesAsync();
            await tx.CommitAsync();

            // 发送通知给创建人
            try
            {
                var creatorUserId = await GetUserIdByNameAsync(order.CreateBy);
                await _notification.SendAsync(creatorUserId, "销售订单已通过",
                    $"订单 {order.OrderNo} 已审批通过，审批人: {approver}",
                    NotificationType.ApprovalReminder, "SaleOrder", order.Id);
            }
            catch
            {
                // 通知发送失败不影响主流程
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            throw new BusinessException("库存数据已被修改，请重试", "CONCURRENCY_ERROR");
        }
        catch (BusinessException)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task RejectAsync(long orderId, string approver, string reason)
    {
        var order = await _uow.SaleOrders.GetByIdWithDetailsAsync(orderId)
            ?? throw new NotFoundException("销售订单不存在");
        if (order.Status != SaleOrderStatus.PendingApproval)
            throw new InvalidOperationException("仅待审批状态可驳回");

        order.Status = SaleOrderStatus.Draft;
        order.Approver = null;
        order.ApproveTime = null;
        order.Remark = string.IsNullOrEmpty(order.Remark)
            ? $"驳回原因: {reason}"
            : $"{order.Remark}\n驳回原因: {reason}";
        order.UpdateBy = approver;
        order.UpdateTime = DateTime.Now;
        _uow.SaleOrders.Update(order);
        await _uow.SaveChangesAsync();

        // 发送驳回通知
        try
        {
            var creatorUserId = await GetUserIdByNameAsync(order.CreateBy);
            await _notification.SendAsync(creatorUserId, "销售订单已驳回",
                $"订单 {order.OrderNo} 已驳回，原因: {reason}",
                NotificationType.ApprovalReminder, "SaleOrder", order.Id);
        }
        catch { }
    }

    public async Task CancelAsync(long orderId, string currentUser)
    {
        var order = await _uow.SaleOrders.GetByIdAsync(orderId)
            ?? throw new NotFoundException("销售订单不存在");
        if (order.Status != SaleOrderStatus.Draft && order.Status != SaleOrderStatus.PendingApproval)
            throw new InvalidOperationException("仅草稿或待审批状态可取消");

        order.Status = SaleOrderStatus.Cancelled;
        order.UpdateBy = currentUser;
        order.UpdateTime = DateTime.Now;
        _uow.SaleOrders.Update(order);
        await _uow.SaveChangesAsync();
    }

    public async Task<decimal> GetCustomerPriceAsync(long customerId, long productId)
    {
        var now = DateTime.Now;
        var customerPrice = await _customerPriceRepo.Query()
            .FirstOrDefaultAsync(p => !p.IsDeleted
                && p.CustomerId == customerId
                && p.ProductId == productId
                && p.EffectiveDate <= now
                && (p.ExpiryDate == null || p.ExpiryDate >= now));

        if (customerPrice != null)
            return customerPrice.SalePrice;

        var product = await _productRepo.Query()
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
        return product?.SalePrice ?? 0;
    }

    // ==================== 出库相关 ====================

    public async Task<List<SaleOrderDetailOutDto>> GetOrderDetailsForOutAsync(long saleOrderId)
    {
        var order = await _uow.SaleOrders.GetByIdWithDetailsAsync(saleOrderId)
            ?? throw new NotFoundException("销售订单不存在");
        if (order.Status != SaleOrderStatus.Approved && order.Status != SaleOrderStatus.PartialShipped)
            throw new InvalidOperationException("仅已审批或部分出库状态的订单可出库");

        var result = new List<SaleOrderDetailOutDto>();
        foreach (var detail in order.Details)
        {
            var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(detail.ProductId, detail.WarehouseId);
            result.Add(new SaleOrderDetailOutDto
            {
                SaleOrderDetailId = detail.Id,
                SaleOrderId = detail.SaleOrderId,
                ProductId = detail.ProductId,
                ProductCode = detail.Product?.Code ?? "",
                ProductName = detail.Product?.Name ?? "",
                WarehouseId = detail.WarehouseId,
                WarehouseName = detail.Warehouse?.Name ?? "",
                OrderQty = detail.Qty,
                ShippedQty = detail.ShippedQty,
                AvailableStock = stock != null ? stock.Quantity - stock.LockQty : 0,
                LockQty = stock?.LockQty ?? 0,
                UnitPrice = detail.UnitPrice
            });
        }
        return result;
    }

    public async Task<SaleOutDto> SaleOutAsync(SaleOutDto dto, string currentUser)
    {
        try
        {
            var order = await _uow.SaleOrders.GetByIdWithDetailsAsync(dto.SaleOrderId)
                ?? throw new NotFoundException("销售订单不存在");
            if (order.Status != SaleOrderStatus.Approved && order.Status != SaleOrderStatus.PartialShipped)
                throw new InvalidOperationException("仅已审批或部分出库状态的订单可出库");

            if (dto.Details.Count == 0)
                throw new InvalidOperationException("出库必须选择至少一条明细");

            // 生成出库单号
            var outNo = await _numberGen.GenerateAsync("SOUT", "Sale_SaleOut", "OutNo");

            // 创建出库单主表
            var saleOut = new SaleSaleOut
            {
                OutNo = outNo,
                SaleOrderId = dto.SaleOrderId,
                CustomerId = order.CustomerId,
                WarehouseId = dto.Details.FirstOrDefault()?.WarehouseId ?? 0,
                OutTime = dto.OutTime,
                TotalAmount = 0,
                Operator = dto.Operator ?? currentUser,
                Remark = dto.Remark,
                CreateBy = currentUser,
                CreateTime = DateTime.Now
            };
            await _outRepo.AddAsync(saleOut);
            await _uow.SaveChangesAsync(); // 拿到 SaleOut.Id

            decimal totalAmount = 0;
            var saleOutDetailRepo = _uow.GetRepository<SaleSaleOutDetail>();

            foreach (var item in dto.Details)
            {
                if (item.Qty <= 0) continue;

                // 查找订单明细
                var orderDetail = order.Details.FirstOrDefault(d => d.Id == item.SaleOrderDetailId)
                    ?? throw new NotFoundException($"订单明细 {item.SaleOrderDetailId} 不存在");

                var remaining = orderDetail.Qty - orderDetail.ShippedQty;

                // 校验出库数量不超过剩余可出库数量
                if (item.Qty > remaining)
                    throw new BusinessException(
                        $"设备 [{orderDetail.Product?.Name}] 本次出库数量 ({item.Qty}) 超过剩余可出库数量 ({remaining})",
                        "OUT_QTY_EXCEEDED");

                // 按 ProductId + WarehouseId 查库存
                var stock = await _uow.Stocks.FindByProductAndWarehouseAsync(orderDetail.ProductId, orderDetail.WarehouseId);
                if (stock == null)
                    throw new BusinessException(
                        $"设备 [{orderDetail.Product?.Name}] 在仓库 [{orderDetail.Warehouse?.Name}] 无库存记录",
                        "STOCK_NOT_FOUND");

                var available = stock.Quantity - stock.LockQty;
                if (available < item.Qty)
                    throw new BusinessException(
                        $"设备 [{orderDetail.Product?.Name}] 可用库存不足（可用: {available}, 需要: {item.Qty}）",
                        "STOCK_INSUFFICIENT");

                // 扣减库存 + 负数保护
                stock.Quantity -= item.Qty;
                if (stock.Quantity < 0) stock.Quantity = 0;

                // 释放锁定库存
                stock.LockQty -= Math.Min(item.Qty, stock.LockQty);
                if (stock.LockQty < 0) stock.LockQty = 0;

                // 生成出库明细
                var outDetail = new SaleSaleOutDetail
                {
                    SaleOutId = saleOut.Id,
                    SaleOrderDetailId = orderDetail.Id,
                    ProductId = orderDetail.ProductId,
                    WarehouseId = orderDetail.WarehouseId,
                    Qty = item.Qty,
                    UnitPrice = orderDetail.UnitPrice,
                    Amount = item.Qty * orderDetail.UnitPrice,
                    CreateBy = currentUser,
                    CreateTime = DateTime.Now
                };
                await saleOutDetailRepo.AddAsync(outDetail);

                // 记录库存流水（出库为负数）
                var balance = stock.Quantity;
                await _uow.StockTransactions.AddAsync(new InvStockTransaction
                {
                    TransactionNo = outNo,
                    TransactionType = TransactionType.SaleOut,
                    ProductId = orderDetail.ProductId,
                    WarehouseId = orderDetail.WarehouseId,
                    Qty = -item.Qty,
                    BalanceQty = balance,
                    RefType = "SaleOut",
                    RefId = saleOut.Id,
                    RefNo = outNo,
                    Operator = currentUser,
                    CreateBy = currentUser,
                    CreateTime = DateTime.Now
                });

                // 更新订单明细已出库数量
                orderDetail.ShippedQty += item.Qty;
                totalAmount += item.Qty * orderDetail.UnitPrice;
            }

            // 更新订单状态
            var allShipped = order.Details.All(d => d.ShippedQty >= d.Qty);
            order.Status = allShipped ? SaleOrderStatus.Shipped : SaleOrderStatus.PartialShipped;
            order.ShippedAmount += totalAmount;
            order.UpdateBy = currentUser;
            order.UpdateTime = DateTime.Now;
            _uow.SaleOrders.Update(order);

            // 更新出库单金额
            saleOut.TotalAmount = totalAmount;
            _outRepo.Update(saleOut);

            await _uow.SaveChangesAsync();
            _stockService.InvalidateStockCache();

            return new SaleOutDto
            {
                Id = saleOut.Id,
                OutNo = saleOut.OutNo,
                SaleOrderId = saleOut.SaleOrderId,
                SaleOrderNo = order.OrderNo,
                CustomerId = saleOut.CustomerId,
                CustomerName = order.Customer?.Name ?? "",
                OutTime = saleOut.OutTime,
                TotalAmount = saleOut.TotalAmount,
                Operator = saleOut.Operator,
                Remark = saleOut.Remark,
                CreateTime = saleOut.CreateTime
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("库存数据已被修改，请刷新重试", "CONCURRENCY_ERROR");
        }
    }

    public async Task<PagedResult<SaleOutListDto>> GetSaleOutListAsync(
        int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate)
    {
        var q = _outRepo.Query()
            .Include(o => o.SaleOrder)
            .Include(o => o.Customer)
            .Where(o => !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(o => o.OutNo.Contains(keyword) || o.SaleOrder.OrderNo.Contains(keyword));
        if (startDate.HasValue)
            q = q.Where(o => o.OutTime >= startDate.Value);
        if (endDate.HasValue)
            q = q.Where(o => o.OutTime <= endDate.Value.AddDays(1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SaleOutListDto>
        {
            Items = items.Select(o => new SaleOutListDto
            {
                Id = o.Id,
                OutNo = o.OutNo,
                SaleOrderId = o.SaleOrderId,
                SaleOrderNo = o.SaleOrder?.OrderNo ?? "",
                CustomerName = o.Customer?.Name ?? "",
                OutTime = o.OutTime,
                TotalAmount = o.TotalAmount,
                Operator = o.Operator,
                CreateTime = o.CreateTime
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SaleOutDto> GetSaleOutByIdAsync(long id)
    {
        var saleOut = await _outRepo.Query()
            .Include(o => o.SaleOrder)
            .Include(o => o.Customer)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .Include(o => o.Details)
                .ThenInclude(d => d.Warehouse)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted)
            ?? throw new NotFoundException("出库单不存在");

        return new SaleOutDto
        {
            Id = saleOut.Id,
            OutNo = saleOut.OutNo,
            SaleOrderId = saleOut.SaleOrderId,
            SaleOrderNo = saleOut.SaleOrder?.OrderNo ?? "",
            CustomerId = saleOut.CustomerId,
            CustomerName = saleOut.Customer?.Name ?? "",
            OutTime = saleOut.OutTime,
            TotalAmount = saleOut.TotalAmount,
            Operator = saleOut.Operator,
            Remark = saleOut.Remark,
            CreateTime = saleOut.CreateTime,
            Details = saleOut.Details.Select(d => new SaleOutDetailDto
            {
                Id = d.Id,
                SaleOutId = d.SaleOutId,
                SaleOrderDetailId = d.SaleOrderDetailId,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Code ?? "",
                ProductName = d.Product?.Name ?? "",
                ProductSpec = d.Product?.Spec ?? "",
                WarehouseId = d.WarehouseId,
                Qty = d.Qty,
                UnitPrice = d.UnitPrice,
                Amount = d.Amount
            }).ToList()
        };
    }

    // ==================== 退货相关 ====================

    public async Task<PagedResult<SaleReturnListDto>> GetReturnListAsync(
        int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate, int? status)
    {
        var returnRepo = _uow.GetRepository<SaleReturn>();
        var q = returnRepo.Query()
            .Include(r => r.SaleOut)
            .Include(r => r.Customer)
            .Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(r => r.ReturnNo.Contains(keyword) || r.SaleOut.OutNo.Contains(keyword));
        if (startDate.HasValue)
            q = q.Where(r => r.ReturnDate >= startDate.Value);
        if (endDate.HasValue)
            q = q.Where(r => r.ReturnDate <= endDate.Value.AddDays(1));
        if (status.HasValue)
            q = q.Where(r => (int)r.Status == status.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SaleReturnListDto>
        {
            Items = items.Select(r => new SaleReturnListDto
            {
                Id = r.Id,
                ReturnNo = r.ReturnNo,
                SaleOutNo = r.SaleOut?.OutNo ?? "",
                CustomerName = r.Customer?.Name ?? "",
                ReturnDate = r.ReturnDate,
                TotalAmount = r.TotalAmount,
                Reason = r.Reason,
                Status = (int)r.Status,
                StatusName = r.Status switch
                {
                    SaleReturnStatus.PendingApproval => "待审批",
                    SaleReturnStatus.Approved => "已审批",
                    SaleReturnStatus.Received => "已入库",
                    SaleReturnStatus.Rejected => "已驳回",
                    _ => ""
                },
                Approver = r.Approver,
                ApproveTime = r.ApproveTime,
                CreateBy = r.CreateBy,
                CreateTime = r.CreateTime
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<SaleReturnDetailDto>> GetOutDetailsForReturnAsync(long saleOutId)
    {
        var saleOutDetailRepo = _uow.GetRepository<SaleSaleOutDetail>();
        var returnDetailRepo = _uow.GetRepository<SaleReturnDetail>();

        var outDetails = await saleOutDetailRepo.Query()
            .Include(d => d.Product)
            .Include(d => d.Warehouse)
            .Where(d => d.SaleOutId == saleOutId && !d.IsDeleted)
            .ToListAsync();

        if (outDetails.Count == 0)
            throw new NotFoundException("出库单不存在或无明细");

        // 查询已退货数量
        var outDetailIds = outDetails.Select(d => d.Id).ToList();
        var returnDetails = await returnDetailRepo.Query()
            .Where(rd => outDetailIds.Contains(rd.SaleOutDetailId) && !rd.IsDeleted)
            .ToListAsync();

        var result = new List<SaleReturnDetailDto>();
        foreach (var d in outDetails)
        {
            var returnedQty = returnDetails
                .Where(rd => rd.SaleOutDetailId == d.Id)
                .Sum(rd => rd.Qty);
            var available = d.Qty - returnedQty;
            if (available <= 0) continue;

            result.Add(new SaleReturnDetailDto
            {
                SaleOutDetailId = d.Id,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Code ?? "",
                ProductName = d.Product?.Name ?? "",
                WarehouseId = d.WarehouseId,
                WarehouseName = d.Warehouse?.Name ?? "",
                OutQty = d.Qty,
                ReturnQty = 0,
                UnitPrice = d.UnitPrice
            });
        }
        return result;
    }

    public async Task<long> CreateReturnAsync(SaleReturnDto dto, string currentUser)
    {
        var saleOut = await _outRepo.Query()
            .Include(o => o.SaleOrder)
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == dto.SaleOutId && !o.IsDeleted)
            ?? throw new NotFoundException("出库单不存在");

        if (dto.Details.Count == 0)
            throw new InvalidOperationException("退货必须选择至少一条明细");

        // 生成退货单号
        var returnNo = await _numberGen.GenerateAsync("SRT", "Sale_Return", "ReturnNo");

        var returnRepo = _uow.GetRepository<SaleReturn>();
        var returnDetailRepo = _uow.GetRepository<SaleReturnDetail>();

        var entity = new SaleReturn
        {
            ReturnNo = returnNo,
            SaleOutId = dto.SaleOutId,
            CustomerId = dto.CustomerId,
            WarehouseId = dto.WarehouseId,
            ReturnDate = dto.ReturnDate,
            TotalAmount = dto.Details.Sum(d => d.ReturnQty * d.UnitPrice),
            Reason = dto.Reason,
            Status = SaleReturnStatus.PendingApproval,
            Operator = dto.Operator ?? currentUser,
            Remark = dto.Remark,
            CreateBy = currentUser,
            CreateTime = DateTime.Now
        };
        await returnRepo.AddAsync(entity);
        await _uow.SaveChangesAsync();

        foreach (var item in dto.Details)
        {
            if (item.ReturnQty <= 0) continue;

            var outDetail = saleOut.Details.FirstOrDefault(d => d.Id == item.SaleOutDetailId)
                ?? throw new NotFoundException($"出库明细 {item.SaleOutDetailId} 不存在");

            if (item.ReturnQty > outDetail.Qty)
                throw new BusinessException(
                    $"设备 [{item.ProductName}] 退货数量 ({item.ReturnQty}) 超过出库数量 ({outDetail.Qty})",
                    "RETURN_QTY_EXCEEDED");

            await returnDetailRepo.AddAsync(new SaleReturnDetail
            {
                SaleReturnId = entity.Id,
                SaleOutDetailId = item.SaleOutDetailId,
                ProductId = item.ProductId,
                WarehouseId = item.WarehouseId,
                Qty = item.ReturnQty,
                UnitPrice = item.UnitPrice,
                Amount = item.ReturnQty * item.UnitPrice,
                CreateBy = currentUser,
                CreateTime = DateTime.Now
            });
        }
        await _uow.SaveChangesAsync();

        return entity.Id;
    }

    public async Task ApproveReturnAsync(long returnId, string approver)
    {
        try
        {
            var returnRepo = _uow.GetRepository<SaleReturn>();
            var returnDetailRepo = _uow.GetRepository<SaleReturnDetail>();

            var entity = await returnRepo.Query()
                .Include(r => r.Details).ThenInclude(d => d.Product)
                .Include(r => r.SaleOut).ThenInclude(o => o.SaleOrder)
                .FirstOrDefaultAsync(r => r.Id == returnId && !r.IsDeleted)
                ?? throw new NotFoundException("退货单不存在");

            if (entity.Status != SaleReturnStatus.PendingApproval)
                throw new InvalidOperationException("仅待审批状态可审批");

            var stockRepo = _uow.Stocks;

            foreach (var detail in entity.Details)
            {
                // 增加库存
                var stock = await stockRepo.FindByProductAndWarehouseAsync(detail.ProductId, detail.WarehouseId);
                if (stock != null)
                {
                    stock.Quantity += detail.Qty;
                }
                else
                {
                    // 如果库存记录不存在则创建
                    var stockEntity = new BaseProductStock
                    {
                        ProductId = detail.ProductId,
                        WarehouseId = detail.WarehouseId,
                        Quantity = detail.Qty,
                        LockQty = 0,
                        CreateBy = approver,
                        CreateTime = DateTime.Now
                    };
                    await _uow.GetRepository<BaseProductStock>().AddAsync(stockEntity);
                }

                // 记录库存流水（退货入库为正数）
                var balance = stock?.Quantity ?? detail.Qty;
                await _uow.StockTransactions.AddAsync(new InvStockTransaction
                {
                    TransactionNo = entity.ReturnNo,
                    TransactionType = TransactionType.SaleReturnIn,
                    ProductId = detail.ProductId,
                    WarehouseId = detail.WarehouseId,
                    Qty = detail.Qty,
                    BalanceQty = balance,
                    RefType = "SaleReturn",
                    RefId = entity.Id,
                    RefNo = entity.ReturnNo,
                    Operator = approver,
                    CreateBy = approver,
                    CreateTime = DateTime.Now
                });
            }

            // 更新退货单状态
            entity.Status = SaleReturnStatus.Received;
            entity.Approver = approver;
            entity.ApproveTime = DateTime.Now;
            entity.UpdateBy = approver;
            entity.UpdateTime = DateTime.Now;
            returnRepo.Update(entity);

            // 更新关联销售订单状态为已退货
            if (entity.SaleOut?.SaleOrder != null)
            {
                entity.SaleOut.SaleOrder.Status = SaleOrderStatus.Returned;
                entity.SaleOut.SaleOrder.UpdateBy = approver;
                entity.SaleOut.SaleOrder.UpdateTime = DateTime.Now;
                _uow.SaleOrders.Update(entity.SaleOut.SaleOrder);
            }

            await _uow.SaveChangesAsync();
            _stockService.InvalidateStockCache();

            // 发送通知
            try
            {
                var creatorUserId = await GetUserIdByNameAsync(entity.CreateBy);
                await _notification.SendAsync(creatorUserId, "退货单已审批",
                    $"退货单 {entity.ReturnNo} 已审批通过并入库，审批人: {approver}",
                    NotificationType.ApprovalReminder, "SaleReturn", entity.Id);
            }
            catch { }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException("库存数据已被修改，请重试", "CONCURRENCY_ERROR");
        }
    }

    public async Task RejectReturnAsync(long returnId, string approver, string reason)
    {
        var returnRepo = _uow.GetRepository<SaleReturn>();

        var entity = await returnRepo.Query()
            .FirstOrDefaultAsync(r => r.Id == returnId && !r.IsDeleted)
            ?? throw new NotFoundException("退货单不存在");

        if (entity.Status != SaleReturnStatus.PendingApproval)
            throw new InvalidOperationException("仅待审批状态可驳回");

        entity.Status = SaleReturnStatus.Rejected;
        entity.Approver = approver;
        entity.ApproveTime = DateTime.Now;
        entity.Remark = string.IsNullOrEmpty(entity.Remark)
            ? $"驳回原因: {reason}"
            : $"{entity.Remark}\n驳回原因: {reason}";
        entity.UpdateBy = approver;
        entity.UpdateTime = DateTime.Now;
        returnRepo.Update(entity);
        await _uow.SaveChangesAsync();
    }

    public async Task<SaleReturnDto> GetReturnByIdAsync(long id)
    {
        var returnRepo = _uow.GetRepository<SaleReturn>();

        var entity = await returnRepo.Query()
            .Include(r => r.SaleOut)
            .Include(r => r.Customer)
            .Include(r => r.Details)
                .ThenInclude(d => d.Product)
            .Include(r => r.Details)
                .ThenInclude(d => d.Warehouse)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted)
            ?? throw new NotFoundException("退货单不存在");

        return new SaleReturnDto
        {
            Id = entity.Id,
            ReturnNo = entity.ReturnNo,
            SaleOutId = entity.SaleOutId,
            SaleOutNo = entity.SaleOut?.OutNo ?? "",
            CustomerId = entity.CustomerId,
            CustomerName = entity.Customer?.Name ?? "",
            WarehouseId = entity.WarehouseId,
            ReturnDate = entity.ReturnDate,
            TotalAmount = entity.TotalAmount,
            Reason = entity.Reason,
            Status = (int)entity.Status,
            StatusName = entity.Status switch
            {
                SaleReturnStatus.PendingApproval => "待审批",
                SaleReturnStatus.Approved => "已审批",
                SaleReturnStatus.Received => "已入库",
                SaleReturnStatus.Rejected => "已驳回",
                _ => ""
            },
            Approver = entity.Approver,
            ApproveTime = entity.ApproveTime,
            Operator = entity.Operator,
            Remark = entity.Remark,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
            Details = entity.Details.Select(d => new SaleReturnDetailDto
            {
                SaleOutDetailId = d.SaleOutDetailId,
                ProductId = d.ProductId,
                ProductCode = d.Product?.Code ?? "",
                ProductName = d.Product?.Name ?? "",
                WarehouseId = d.WarehouseId,
                ReturnQty = d.Qty,
                OutQty = 0,
                UnitPrice = d.UnitPrice
            }).ToList()
        };
    }

    // ==================== 私有方法 ====================

    private async Task<long> GetUserIdByNameAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName)) return 0;
        var user = await _db.SysUsers.FirstOrDefaultAsync(u => u.UserName == userName);
        return user?.Id ?? 0;
    }

    private async Task CheckCreditAsync(long customerId, decimal totalAmount)
    {
        var enabled = await _config.GetAsync("CreditCheckEnabled");
        if (enabled != "true") return;

        var customer = await _customerRepo.Query()
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted)
            ?? throw new NotFoundException("客户不存在");

        if (customer.UsedCredit + totalAmount > customer.CreditLimit)
            throw new BusinessException("客户信用额度不足", "CREDIT_EXCEEDED");
    }

    private static SaleOrderListDto ToListDto(SaleSaleOrder o) => new()
    {
        Id = o.Id,
        OrderNo = o.OrderNo,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.Name ?? "",
        OrderDate = o.OrderDate,
        ExpectedDate = o.ExpectedDate,
        TotalAmount = o.TotalAmount,
        ShippedAmount = o.ShippedAmount,
        Status = o.Status,
        CreateBy = o.CreateBy,
        CreateTime = o.CreateTime
    };

    private static SaleOrderDto ToDto(SaleSaleOrder o) => new()
    {
        Id = o.Id,
        OrderNo = o.OrderNo,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.Name ?? "",
        OrderDate = o.OrderDate,
        ExpectedDate = o.ExpectedDate,
        TotalAmount = o.TotalAmount,
        ShippedAmount = o.ShippedAmount,
        Status = (int)o.Status,
        StatusName = o.Status switch
        {
            SaleOrderStatus.Draft => "草稿",
            SaleOrderStatus.PendingApproval => "待审批",
            SaleOrderStatus.Approved => "已审批",
            SaleOrderStatus.PartialShipped => "部分出库",
            SaleOrderStatus.Shipped => "已出库",
            SaleOrderStatus.Returned => "已退货",
            SaleOrderStatus.Cancelled => "已取消",
            _ => ""
        },
        Approver = o.Approver,
        ApproveTime = o.ApproveTime,
        Remark = o.Remark,
        CreateBy = o.CreateBy,
        CreateTime = o.CreateTime,
        Details = o.Details.Select(d => new SaleOrderDetailDto
        {
            Id = d.Id,
            SaleOrderId = d.SaleOrderId,
            ProductId = d.ProductId,
            ProductCode = d.Product?.Code ?? "",
            ProductName = d.Product?.Name ?? "",
            Spec = d.Product?.Spec,
            Unit = d.Product?.Unit,
            WarehouseId = d.WarehouseId,
            WarehouseName = d.Warehouse?.Name ?? "",
            Qty = d.Qty,
            ShippedQty = d.ShippedQty,
            UnitPrice = d.UnitPrice,
            Amount = d.Amount,
            Remark = d.Remark
        }).ToList()
    };
}
