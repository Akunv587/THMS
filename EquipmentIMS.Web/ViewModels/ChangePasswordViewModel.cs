using System.ComponentModel.DataAnnotations;

namespace EquipmentIMS.Web.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "请输入原密码")]
    [DataType(DataType.Password)]
    [Display(Name = "原密码")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入新密码")]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "密码长度8-50位")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "密码必须包含大小写字母和数字")]
    [DataType(DataType.Password)]
    [Display(Name = "新密码")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "请确认新密码")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
    [Display(Name = "确认新密码")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
