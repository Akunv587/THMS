using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.ViewModels;

namespace EquipmentIMS.Web.Controllers;

/// <summary>
/// 系统配置管理控制器
/// </summary>
[PermissionFilter("sys:config:view")]
public class SystemConfigController : Controller
{
    private readonly ISystemConfigService _service;
    private readonly ICacheService _cache;

    public SystemConfigController(ISystemConfigService service, ICacheService cache)
    {
        _service = service;
        _cache = cache;
    }

    /// <summary>
    /// 配置列表页 — 按分组只读预览
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var configs = await _service.GetAllForEditAsync();
        // 注：Index 需要包含所有配置项（含不可编辑的），这里复用 GetAllForEditAsync 不够，
        // 改用 GetAllAsync 获取实体后手动映射
        var allEntities = await _service.GetAllAsync();
        var allConfigs = allEntities.Select(c => new SystemConfigDto
        {
            Id = c.Id,
            ConfigKey = c.ConfigKey,
            ConfigValue = c.ConfigValue,
            ConfigName = c.ConfigName,
            Description = c.Description,
            ConfigType = (int)c.ConfigType,
            IsEditable = c.IsEditable,
            UpdateTime = c.UpdateTime
        }).ToList();

        var vm = BuildViewModel(allConfigs);
        return View(vm);
    }

    /// <summary>
    /// 配置编辑页 — 按分组表单编辑
    /// </summary>
    [HttpGet("Edit")]
    [PermissionFilter("sys:config:edit")]
    public async Task<IActionResult> Edit()
    {
        var configs = await _service.GetAllForEditAsync();
        var vm = BuildViewModel(configs);
        return View(vm);
    }

    /// <summary>
    /// 批量保存配置 — 逐键更新并清除缓存
    /// </summary>
    [HttpPost("Edit")]
    [PermissionFilter("sys:config:edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Dictionary<string, string> configValues)
    {
        foreach (var kv in configValues)
        {
            // 跳过空的验证令牌字段
            if (kv.Key.StartsWith("__")) continue;

            await _service.SetAsync(kv.Key, kv.Value);
        }

        // 批量清除所有 SysConfig 缓存，确保实时生效
        await _cache.RemoveByPatternAsync("SysConfig:");

        TempData["Message"] = "配置已更新，缓存已刷新";
        return RedirectToAction(nameof(Index));
    }

    #region 配置分组映射

    /// <summary>配置键对应的分组信息</summary>
    private sealed record ConfigGroupInfo(string GroupName, string GroupIcon);

    /// <summary>配置键 → 分组归属映射表</summary>
    private static readonly Dictionary<string, ConfigGroupInfo> KeyGroupMap = new()
    {
        // 分组一：公司信息
        ["CompanyName"] = new("公司信息", "bi-building"),
        ["CompanyAddress"] = new("公司信息", "bi-building"),
        ["CompanyPhone"] = new("公司信息", "bi-building"),
        ["CompanyEmail"] = new("公司信息", "bi-building"),

        // 分组二：单据设置
        ["Prefix_PurchaseOrder"] = new("单据设置", "bi-file-text"),
        ["Prefix_SaleOrder"] = new("单据设置", "bi-file-text"),
        ["Prefix_PurchaseIn"] = new("单据设置", "bi-file-text"),
        ["Prefix_SaleOut"] = new("单据设置", "bi-file-text"),
        ["Prefix_RepairOrder"] = new("单据设置", "bi-file-text"),

        // 分组三：安全策略
        ["PasswordExpiryDays"] = new("安全策略", "bi-shield-check"),
        ["MaxLoginAttempts"] = new("安全策略", "bi-shield-check"),
        ["LockoutMinutes"] = new("安全策略", "bi-shield-check"),
        ["PasswordHistoryCount"] = new("安全策略", "bi-shield-check"),
        ["CreditCheckEnabled"] = new("安全策略", "bi-shield-check"),

        // 分组四：预警设置
        ["StockAlertCheckCron"] = new("预警设置", "bi-bell"),
        ["MaintenanceReminderDays"] = new("预警设置", "bi-bell"),
    };

    /// <summary>分组展示顺序（保持稳定）</summary>
    private static readonly List<string> GroupOrder = new()
    {
        "公司信息", "单据设置", "安全策略", "预警设置"
    };

    private static SystemConfigEditViewModel BuildViewModel(List<SystemConfigDto> configs)
    {
        var groupDict = new Dictionary<string, ConfigGroupViewModel>();

        foreach (var cfg in configs)
        {
            // 查找分组归属，未匹配的归入"其他设置"
            var groupInfo = KeyGroupMap.TryGetValue(cfg.ConfigKey, out var info)
                ? info
                : new ConfigGroupInfo("其他设置", "bi-gear");

            if (!groupDict.ContainsKey(groupInfo.GroupName))
            {
                groupDict[groupInfo.GroupName] = new ConfigGroupViewModel
                {
                    GroupName = groupInfo.GroupName,
                    GroupIcon = groupInfo.GroupIcon
                };
            }

            groupDict[groupInfo.GroupName].Items.Add(new ConfigItemViewModel
            {
                Id = cfg.Id,
                ConfigKey = cfg.ConfigKey,
                ConfigName = cfg.ConfigName,
                ConfigValue = cfg.ConfigValue,
                Description = cfg.Description,
                ConfigType = cfg.ConfigType,
                IsEditable = cfg.IsEditable
            });
        }

        // 按 GroupOrder 稳定排序
        var orderedGroups = GroupOrder
            .Where(g => groupDict.ContainsKey(g))
            .Select(g => groupDict[g])
            .ToList();

        // 追加未在排序列表中的分组（如"其他设置"）
        foreach (var g in groupDict.Values)
        {
            if (!orderedGroups.Contains(g))
                orderedGroups.Add(g);
        }

        return new SystemConfigEditViewModel { Groups = orderedGroups };
    }

    #endregion
}
