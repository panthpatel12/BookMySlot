using BookMySlot.Data;
using BookMySlot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookMySlot.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private const string CacheKey = "bookmyslot_active_services";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public ServiceCatalogService(ApplicationDbContext dbContext, IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _memoryCache = memoryCache;
    }

    public async Task<IReadOnlyList<Service>> GetActiveServicesAsync(bool bypassCache = false)
    {
        if (bypassCache)
        {
            return await _dbContext.Services
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        return await _memoryCache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _dbContext.Services
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }) ?? (IReadOnlyList<Service>)Array.Empty<Service>();
    }

    public async Task<Service?> GetServiceByIdAsync(int id)
    {
        return await _dbContext.Services
            .AsNoTracking()
            .Include(s => s.ProviderServices)
                .ThenInclude(ps => ps.ProviderProfile)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public void InvalidateCache()
    {
        _memoryCache.Remove(CacheKey);
    }
}
