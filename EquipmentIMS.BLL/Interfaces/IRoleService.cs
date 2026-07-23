using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IRoleService
{
    Task<List<RoleListDto>> GetListAsync();
    Task<RoleCreateEditDto?> GetByIdAsync(long id);
    Task<long> CreateAsync(RoleCreateEditDto dto);
    Task UpdateAsync(RoleCreateEditDto dto);
    Task DeleteAsync(long id);
    Task<AssignPermissionsDto> GetAssignPermissionsAsync(long id);
    Task AssignPermissionsAsync(long roleId, List<long> permissionIds);
}
