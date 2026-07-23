namespace EquipmentIMS.BLL.Interfaces;

/// <summary>
/// 当前用户服务接口，从 HttpContext Claims 获取用户信息
/// </summary>
public interface ICurrentUserService
{
    string UserName { get; }
    long UserId { get; }
    string RealName { get; }
    List<string> Roles { get; }
    bool IsInRole(string roleCode);
}
