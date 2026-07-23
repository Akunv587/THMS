using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using EquipmentIMS.BLL.Interfaces;

namespace EquipmentIMS.BLL.Services;

/// <summary>
/// 当前用户服务，从 JWT Claims 中解析用户身份
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName => GetClaimValue(ClaimTypes.Name) ?? "anonymous";

    public long UserId
    {
        get
        {
            var val = GetClaimValue(ClaimTypes.NameIdentifier);
            return long.TryParse(val, out var id) ? id : 0;
        }
    }

    public string RealName => GetClaimValue("RealName") ?? UserName;

    public List<string> Roles
    {
        get
        {
            var roles = GetClaimValue(ClaimTypes.Role);
            return string.IsNullOrEmpty(roles)
                ? new List<string>()
                : roles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }

    public bool IsInRole(string roleCode)
    {
        return Roles.Contains(roleCode, StringComparer.OrdinalIgnoreCase);
    }

    private string? GetClaimValue(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
    }
}
