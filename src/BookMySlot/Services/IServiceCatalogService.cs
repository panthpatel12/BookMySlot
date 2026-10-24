using BookMySlot.Models;

namespace BookMySlot.Services;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<Service>> GetActiveServicesAsync(bool bypassCache = false);
    Task<Service?> GetServiceByIdAsync(int id);
    void InvalidateCache();
}
