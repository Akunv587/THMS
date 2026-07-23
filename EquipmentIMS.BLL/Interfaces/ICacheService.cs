namespace EquipmentIMS.BLL.Interfaces;

/// <summary>
/// 缓存服务接口
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);

    /// <summary>
    /// 按 key 前缀批量移除缓存项
    /// </summary>
    Task RemoveByPatternAsync(string pattern);
}
