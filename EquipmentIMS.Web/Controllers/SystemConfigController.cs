using Microsoft.AspNetCore.Mvc;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Web.Filters;
using EquipmentIMS.Web.ViewModels;

namespace EquipmentIMS.Web.Controllers;

/// <summary>
/// 系统配置管理控制器
/// </summary>
[Route("[controller]")]
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
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
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
            if (kv.Key.StartsWith("__")) continue;
            await _service.SetAsync(kv.Key, kv.Value);
        }

        await _cache.RemoveByPatternAsync("SysConfig:");
        TempData["Message"] = "配置已更新，缓存已刷新";
        return RedirectToAction(nameof(Index));
    }

    #region 配置分组映射

    private sealed record ConfigGroupInfo(string GroupName, string GroupIcon);

    private static readonly Dictionary<string, ConfigGroupInfo> KeyGroupMap = new()
    {
        ["CompanyName"] = new("公司信息", "bi-building"),
        ["CompanyAddress"] = new("公司信息", "bi-building"),
        ["CompanyPhone"] = new("公司信息", "bi-building"),
        ["CompanyEmail"] = new("公司信息", "bi-building"),

        ["Prefix_PurchaseOrder"] = new("单据设置", "bi-file-text"),
        ["Prefix_SaleOrder"] = new("单据设置", "bi-file-text"),
        ["Prefix_PurchaseIn"] = new("单据设置", "bi-file-text"),
        ["Prefix_SaleOut"] = new("单据设置", "bi-file-text"),
        ["Prefix_RepairOrder"] = new("单据设置", "bi-file-text"),

        ["PasswordExpiryDays"] = new("安全策略", "bi-shield-check"),
        ["MaxLoginAttempts"] = new("安全策略", "bi-shield-check"),
        ["LockoutMinutes"] = new("安全策略", "bi-shield-check"),
        ["PasswordHistoryCount"] = new("安全策略", "bi-shield-check"),
        ["CreditCheckEnabled"] = new("安全策略", "bi-shield-check"),

        ["StockAlertCheckCron"] = new("预警设置", "bi-bell"),
        ["MaintenanceReminderDays"] = new("预警设置", "bi-bell"),
    };

    private static readonly List<string> GroupOrder = new()
    {
        "公司信息", "单据设置", "安全策略", "预警设置"
    };

    private static SystemConfigEditViewModel BuildViewModel(List<SystemConfigDto> configs)
    {
        var groupDict = new Dictionary<string, ConfigGroupViewModel>();

        foreach (var cfg in configs)
        {
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

        var orderedGroups = GroupOrder
            .Where(g => groupDict.ContainsKey(g))
            .Select(g => groupDict[g])
            .ToList();

        foreach (var g in groupDict.Values)
        {
            if (!orderedGroups.Contains(g))
                orderedGroups.Add(g);
        }

        return new SystemConfigEditViewModel { Groups = orderedGroups };
    }

    #endregion
}
