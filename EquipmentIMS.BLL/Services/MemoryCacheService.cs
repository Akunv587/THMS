using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using EquipmentIMS.BLL.Interfaces;

namespace EquipmentIMS.BLL.Services;

/// <summary>
/// 基于 IMemoryCache 的缓存服务，支持按前缀批量移除
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    // 维护 key → 集合，支持按前缀查询
    private static readonly ConcurrentDictionary<string, HashSet<string>> _keyIndex = new(StringComparer.OrdinalIgnoreCase);

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        _cache.Set(key, value, expiration);
        _keyIndex.AddOrUpdate(key,
            _ => new HashSet<string> { key },
            (_, set) => { set.Add(key); return set; });
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _keyIndex.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        var keysToRemove = _keyIndex.Keys
            .Where(k => k.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keyIndex.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
