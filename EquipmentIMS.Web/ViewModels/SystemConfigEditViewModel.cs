namespace EquipmentIMS.Web.ViewModels;

/// <summary>
/// 系统配置编辑页视图模型 — 分组展示所有可编辑配置项
/// </summary>
public class SystemConfigEditViewModel
{
    public List<ConfigGroupViewModel> Groups { get; set; } = new();
}

/// <summary>
/// 配置分组视图模型
/// </summary>
public class ConfigGroupViewModel
{
    /// <summary>分组名称：公司信息 / 单据设置 / 安全策略 / 预警设置</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Bootstrap Icons 类名</summary>
    public string GroupIcon { get; set; } = "bi-gear";

    /// <summary>该分组下的配置项列表</summary>
    public List<ConfigItemViewModel> Items { get; set; } = new();
}

/// <summary>
/// 单个配置项视图模型
/// </summary>
public class ConfigItemViewModel
{
    public long Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigName { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
    public string? Description { get; set; }

    /// <summary>控件类型：1=文本, 2=数字, 3=布尔/开关, 4=JSON</summary>
    public int ConfigType { get; set; }

    /// <summary>是否允许编辑</summary>
    public bool IsEditable { get; set; }
}
