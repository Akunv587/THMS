using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserListDto>> GetListAsync(UserQueryDto query);
    Task<UserCreateEditDto?> GetByIdAsync(long id);
    Task<long> CreateAsync(UserCreateEditDto dto);
    Task UpdateAsync(UserCreateEditDto dto);
    Task DeleteAsync(long id);
    Task ResetPasswordAsync(long id, string newPassword);
    Task ToggleActiveAsync(long id);
    Task<AssignRolesDto> GetAssignRolesAsync(long id);
    Task AssignRolesAsync(long userId, List<long> roleIds);
    Task<List<RoleOptionDto>> GetAllRolesAsync();

    // 密码历史相关 (C.3)
    Task<List<SysPasswordHistory>> GetPasswordHistoriesAsync(long userId, int count);
    Task AddPasswordHistoryAsync(SysPasswordHistory history);
    Task TrimPasswordHistoryAsync(long userId, int keepCount);
}
