using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Models.DTOs;

public class RoleCreateEditDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "角色编码不能为空"), MaxLength(50)]
    [Display(Name = "角色编码")]
    public string RoleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "角色名称不能为空"), MaxLength(50)]
    [Display(Name = "角色名称")]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "描述")]
    public string? Description { get; set; }
}

public class RoleListDto
{
    public long Id { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
    public DateTime CreateTime { get; set; }
}

public class PermissionTreeDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public int PermissionType { get; set; }
    public string PermissionTypeName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsChecked { get; set; }
    public List<PermissionTreeDto> Children { get; set; } = new();
}

public class AssignPermissionsDto
{
    public long RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionTreeDto> PermissionTree { get; set; } = new();
}

public class SavePermissionsRequest
{
    public long RoleId { get; set; }
    public List<long> PermissionIds { get; set; } = new();
}
