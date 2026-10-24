using BookMySlot.ViewModels.Common;

namespace BookMySlot.ViewModels.Customer;

public class CustomerDashboardViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public int UpcomingCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalBookingsCount { get; set; }
    public List<AppointmentListItemViewModel> UpcomingAppointments { get; set; } = new();
    public List<AppointmentListItemViewModel> RecentHistory { get; set; } = new();
    public string TimeZoneDisplayName { get; set; } = string.Empty;
}
