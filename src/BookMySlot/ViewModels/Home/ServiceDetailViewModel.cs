using BookMySlot.Models;

namespace BookMySlot.ViewModels.Home;

public class ProviderSummaryItem
{
    public int ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
}

public class ServiceDetailViewModel
{
    public Service Service { get; set; } = null!;
    public List<ProviderSummaryItem> AssignedProviders { get; set; } = new();
    public List<PublicAvailableSlotItem> AvailableSlots { get; set; } = new();
    public string TimeZoneDisplayName { get; set; } = string.Empty;
}
