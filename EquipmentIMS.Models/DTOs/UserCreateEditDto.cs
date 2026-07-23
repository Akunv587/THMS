using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Models.DTOs;

public class UserCreateEditDto
{
    public long Id { get; set; }

    [Required(ErrorMessage = "用户名不能为空"), MaxLength(50)]
    [Display(Name = "用户名")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "真实姓名不能为空"), MaxLength(50)]
    [Display(Name = "真实姓名")]
    public string RealName { get; set; } = string.Empty;

    [MaxLength(100), EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [Display(Name = "邮箱")]
    public string? Email { get; set; }

    [MaxLength(20), Phone(ErrorMessage = "手机号格式不正确")]
    [Display(Name = "手机号")]
    public string? Phone { get; set; }

    [Display(Name = "启用")]
    public bool IsActive { get; set; } = true;
}

public class UserListDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime? LastLoginTime { get; set; }
    public DateTime CreateTime { get; set; }
}

public class UserQueryDto
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AssignRolesDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public List<long> SelectedRoleIds { get; set; } = new();
    public List<RoleOptionDto> AllRoles { get; set; } = new();
}

public class RoleOptionDto
{
    public long Id { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class ResetPasswordDto
{
    [Required(ErrorMessage = "新密码不能为空"), MinLength(6, ErrorMessage = "密码至少6位")]
    [DataType(DataType.Password)]
    [Display(Name = "新密码")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "两次密码输入不一致")]
    [Display(Name = "确认密码")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
