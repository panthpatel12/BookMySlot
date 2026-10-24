using BookMySlot.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookMySlot.ViewModels.Home;

public class PublicAvailableSlotItem
{
    public int SlotId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string FormattedSchedule { get; set; } = string.Empty;
    public string FormattedDate { get; set; } = string.Empty;
    public string FormattedTime { get; set; } = string.Empty;
}

public class HomeIndexViewModel
{
    public int? SelectedServiceId { get; set; }
    public int? SelectedProviderId { get; set; }
    public DateTime? SelectedDate { get; set; }

    public List<SelectListItem> ServicesList { get; set; } = new();
    public List<SelectListItem> ProvidersList { get; set; } = new();

    public IReadOnlyList<Service> FeaturedServices { get; set; } = new List<Service>();
    public List<PublicAvailableSlotItem> AvailableSlots { get; set; } = new();

    public int TotalServicesCount { get; set; }
    public int TotalProvidersCount { get; set; }
    public int TotalBookingsCount { get; set; }
    public string TimeZoneDisplayName { get; set; } = string.Empty;
}
