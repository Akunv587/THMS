using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using EquipmentIMS.BLL.Interfaces;
using EquipmentIMS.DAL.Interfaces;
using EquipmentIMS.Models.DTOs;
using EquipmentIMS.Models.Entities;

namespace EquipmentIMS.BLL.Services;

/// <summary>
/// 系统配置服务，使用 IMemoryCache 30分钟缓存
/// </summary>
public class SystemConfigService : ISystemConfigService
{
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public SystemConfigService(IUnitOfWork uow, IMemoryCache cache)
    {
        _uow = uow;
        _cache = cache;
    }

    public async Task<string?> GetAsync(string key)
    {
        var cacheKey = $"SysConfig:{key}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var config = await _uow.GetRepository<SysConfig>()
            .Query()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        var value = config?.ConfigValue;
        _cache.Set(cacheKey, value, CacheDuration);
        return value;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await GetAsync(key);
        if (string.IsNullOrEmpty(value)) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        var repo = _uow.GetRepository<SysConfig>();
        var config = await repo.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        if (config != null)
        {
            config.ConfigValue = value;
            config.UpdateTime = DateTime.Now;
            repo.Update(config);
        }
        else
        {
            await repo.AddAsync(new SysConfig
            {
                ConfigKey = key,
                ConfigValue = value,
                ConfigName = key,
                IsEditable = true
            });
        }

        await _uow.SaveChangesAsync();
        _cache.Set($"SysConfig:{key}", value, CacheDuration);
    }

    public async Task<List<SysConfig>> GetAllAsync()
    {
        return await _uow.GetRepository<SysConfig>()
            .Query()
            .ToListAsync();
    }

    public async Task<List<SystemConfigDto>> GetAllForEditAsync()
    {
        return await _uow.GetRepository<SysConfig>()
            .Query()
            .Where(c => c.IsEditable)
            .Select(c => new SystemConfigDto
            {
                Id = c.Id,
                ConfigKey = c.ConfigKey,
                ConfigValue = c.ConfigValue,
                ConfigName = c.ConfigName,
                Description = c.Description,
                ConfigType = (int)c.ConfigType,
                IsEditable = c.IsEditable,
                UpdateTime = c.UpdateTime
            })
            .ToListAsync();
    }

    public async Task UpdateAsync(List<SystemConfigDto> configs)
    {
        var repo = _uow.GetRepository<SysConfig>();
        var keys = configs.Select(c => c.ConfigKey).ToList();
        var entities = await repo.Query().Where(c => keys.Contains(c.ConfigKey)).ToListAsync();

        foreach (var dto in configs)
        {
            var entity = entities.FirstOrDefault(e => e.ConfigKey == dto.ConfigKey);
            if (entity != null && entity.IsEditable)
            {
                entity.ConfigValue = dto.ConfigValue;
                entity.UpdateTime = DateTime.Now;
                repo.Update(entity);
                _cache.Remove($"SysConfig:{dto.ConfigKey}");
            }
        }

        await _uow.SaveChangesAsync();
    }
}
