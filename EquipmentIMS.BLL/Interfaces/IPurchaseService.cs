using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IPurchaseService
{
    Task<PagedResult<PurchaseOrderListDto>> GetListAsync(PurchaseOrderQueryDto query);
    Task<PurchaseOrderDto> GetByIdAsync(long id);
    Task<long> CreateAsync(PurchaseOrderDto dto, string currentUser);
    Task UpdateAsync(PurchaseOrderDto dto, string currentUser);
    Task SubmitAsync(long orderId, string currentUser);
    Task ApproveAsync(long orderId, long approverId, string approverName);
    Task RejectAsync(long orderId, long approverId, string approverName, string reason);
    Task CancelAsync(long orderId, string currentUser);

    // 采购入库
    Task<PagedResult<PurchaseInListDto>> GetInListAsync(PurchaseInQueryDto query);
    Task<long> PurchaseInAsync(PurchaseInRequestDto dto, string currentUser);

    // 采购退货
    Task<PagedResult<PurchaseReturnListDto>> GetReturnListAsync(PurchaseReturnQueryDto query);
    Task<long> PurchaseReturnAsync(PurchaseReturnRequestDto dto, string currentUser);
}
