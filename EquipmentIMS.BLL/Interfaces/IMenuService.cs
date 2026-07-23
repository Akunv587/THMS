using EquipmentIMS.Models.DTOs;

namespace EquipmentIMS.BLL.Interfaces;

public interface IMenuService
{
    Task<List<MenuDto>> GetMenusByUserAsync(long userId);
    Task ClearMenuCacheAsync(long userId);
}
