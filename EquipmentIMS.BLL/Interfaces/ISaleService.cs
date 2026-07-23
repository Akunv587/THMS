using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface ISaleService
{
    Task<PagedResult<SaleOrderListDto>> GetListAsync(SaleOrderQueryDto query, string currentUser, bool isManager);
    Task<SaleOrderDto> GetByIdAsync(long id);
    Task<long> CreateAsync(SaleOrderDto dto, string currentUser);
    Task UpdateAsync(SaleOrderDto dto, string currentUser);
    Task SubmitAsync(long orderId, string currentUser);
    Task ApproveAsync(long orderId, string approver);
    Task RejectAsync(long orderId, string approver, string reason);
    Task CancelAsync(long orderId, string currentUser);
    Task<decimal> GetCustomerPriceAsync(long customerId, long productId);

    /// <summary>获取可用于出库的订单明细（含库存信息）</summary>
    Task<List<SaleOrderDetailOutDto>> GetOrderDetailsForOutAsync(long saleOrderId);

    /// <summary>销售出库（含部分出库、库存扣减、锁定释放、流水记录）</summary>
    Task<SaleOutDto> SaleOutAsync(SaleOutDto dto, string currentUser);

    /// <summary>出库单列表</summary>
    Task<PagedResult<SaleOutListDto>> GetSaleOutListAsync(int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate);

    /// <summary>出库单详情</summary>
    Task<SaleOutDto> GetSaleOutByIdAsync(long id);

    // ==================== 退货相关 ====================

    /// <summary>退货单列表</summary>
    Task<PagedResult<SaleReturnListDto>> GetReturnListAsync(int page, int pageSize, string? keyword, DateTime? startDate, DateTime? endDate, int? status);

    /// <summary>获取可用于退货的出库明细</summary>
    Task<List<SaleReturnDetailDto>> GetOutDetailsForReturnAsync(long saleOutId);

    /// <summary>创建退货单</summary>
    Task<long> CreateReturnAsync(SaleReturnDto dto, string currentUser);

    /// <summary>审批退货（增加库存、记录流水、更新出库单状态）</summary>
    Task ApproveReturnAsync(long returnId, string approver);

    /// <summary>驳回退货</summary>
    Task RejectReturnAsync(long returnId, string approver, string reason);

    /// <summary>退货单详情</summary>
    Task<SaleReturnDto> GetReturnByIdAsync(long id);
}
