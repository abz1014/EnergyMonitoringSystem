namespace EMS.Web.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class AppSettingsService
{
    private readonly ScadaDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "app_settings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AppSettingsService(ScadaDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, string>? cached) && cached != null)
            return cached;

        var settings = await _context.AppSettings.AsNoTracking().ToListAsync();
        var dict = settings.ToDictionary(s => s.SettingKey, s => s.SettingValue ?? "");
        _cache.Set(CacheKey, dict, CacheDuration);
        return dict;
    }

    public async Task<List<AppSetting>> GetAllSettingsAsync()
    {
        return await _context.AppSettings.AsNoTracking().OrderBy(s => s.Category).ThenBy(s => s.SettingKey).ToListAsync();
    }

    public async Task<string> GetAsync(string key, string defaultValue = "")
    {
        var all = await GetAllAsync();
        return all.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public async Task<double> GetDoubleAsync(string key, double defaultValue = 0)
    {
        var val = await GetAsync(key);
        return double.TryParse(val, out var result) ? result : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var val = await GetAsync(key);
        return int.TryParse(val, out var result) ? result : defaultValue;
    }

    public async Task UpdateAsync(string key, string value, string updatedBy)
    {
        var setting = await _context.AppSettings.FindAsync(key);
        if (setting != null)
        {
            setting.SettingValue = value;
            setting.UpdatedAt = DateTime.Now;
            setting.UpdatedBy = updatedBy;
            await _context.SaveChangesAsync();
            _cache.Remove(CacheKey);
        }
    }

    public async Task UpdateMultipleAsync(Dictionary<string, string> updates, string updatedBy)
    {
        foreach (var kvp in updates)
        {
            var setting = await _context.AppSettings.FindAsync(kvp.Key);
            if (setting != null)
            {
                setting.SettingValue = kvp.Value;
                setting.UpdatedAt = DateTime.Now;
                setting.UpdatedBy = updatedBy;
            }
        }
        await _context.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }
}
