using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Enums;

namespace EquipmentIMS.Web.Controllers;

/// <summary>
/// 消息通知控制器
/// </summary>
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public NotificationController(INotificationService notificationService, ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 通知列表页（支持按类型筛选 + 分页）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? type = null)
    {
        if (page < 1) page = 1;

        var query = new NotificationQueryDto
        {
            Page = page,
            PageSize = 20
        };

        // 映射 type 筛选参数
        if (!string.IsNullOrEmpty(type))
        {
            switch (type.ToLowerInvariant())
            {
                case "unread":
                    query.IsRead = false;
                    break;
                case "approval":
                    query.NotificationType = (int)NotificationType.ApprovalReminder;
                    break;
                case "stock-alert":
                    query.NotificationType = (int)NotificationType.StockAlert;
                    break;
                case "maintenance":
                    query.NotificationType = (int)NotificationType.MaintenanceReminder;
                    break;
                case "system":
                    query.NotificationType = (int)NotificationType.SystemNotification;
                    break;
            }
        }

        var result = await _notificationService.GetListAsync(_currentUser.UserId, query);
        ViewData["TypeFilter"] = type ?? "all";
        return View(result);
    }

    /// <summary>
    /// 通知详情页（打开时自动标记已读）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null || notification.UserId != _currentUser.UserId)
            return NotFound();

        // 打开详情页时自动标记为已读
        await _notificationService.MarkAsReadAsync(id);

        // 构造关联业务跳转 URL
        ViewData["RefUrl"] = BuildRefUrl(notification.RefType, notification.RefId);
        ViewData["RefLabel"] = BuildRefLabel(notification.RefType);

        return View(notification);
    }

    /// <summary>
    /// Ajax：获取当前用户未读数量（导航栏轮询用）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(_currentUser.UserId);
        return Json(ApiResponse.Ok(count));
    }

    /// <summary>
    /// Ajax：获取最近 5 条未读通知（导航栏下拉用）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecentUnread()
    {
        var items = await _notificationService.GetRecentUnreadAsync(_currentUser.UserId, 5);
        return Json(ApiResponse.Ok(items));
    }

    /// <summary>
    /// Ajax：标记单条已读
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Json(ApiResponse.Ok("已标记为已读"));
    }

    /// <summary>
    /// Ajax：全部标记已读
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notificationService.MarkAllAsReadAsync(_currentUser.UserId);
        return Json(ApiResponse.Ok("全部标记为已读"));
    }

    #region 关联业务跳转辅助方法

    /// <summary>
    /// 根据 RefType + RefId 生成关联业务页面的 URL
    /// </summary>
    private static string? BuildRefUrl(string? refType, long? refId)
    {
        if (string.IsNullOrEmpty(refType) || refId == null)
            return null;

        return refType switch
        {
            "PurchaseOrder" => $"/PurchaseOrder/Details/{refId}",
            "PurchaseIn" => $"/PurchaseIn/Details/{refId}",
            "PurchaseReturn" => $"/PurchaseReturn/Details/{refId}",
            "SaleOrder" => $"/SaleOrder/Details/{refId}",
            "SaleOut" => $"/SaleOut/Details/{refId}",
            "SaleReturn" => $"/SaleReturn/Details/{refId}",
            "RepairOrder" => $"/RepairOrder/Details/{refId}",
            "MaintenancePlan" => $"/MaintenancePlan/Details/{refId}",
            "StockCheck" => $"/StockCheck/Details/{refId}",
            "StockTransfer" => $"/StockTransfer/Details/{refId}",
            "DeviceArchive" => $"/DeviceArchive/Details/{refId}",
            _ => null
        };
    }

    /// <summary>
    /// 根据 RefType 生成业务名称标签
    /// </summary>
    private static string? BuildRefLabel(string? refType)
    {
        return refType switch
        {
            "PurchaseOrder" => "采购订单",
            "PurchaseIn" => "采购入库单",
            "PurchaseReturn" => "采购退货单",
            "SaleOrder" => "销售订单",
            "SaleOut" => "销售出库单",
            "SaleReturn" => "销售退货单",
            "RepairOrder" => "维修工单",
            "MaintenancePlan" => "保养计划",
            "StockCheck" => "盘点单",
            "StockTransfer" => "调拨单",
            "DeviceArchive" => "设备档案",
            _ => refType
        };
    }

    #endregion
}
