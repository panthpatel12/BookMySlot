using BookMySlot.ViewModels.Common;

namespace BookMySlot.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalProviders { get; set; }
    public int TotalServices { get; set; }
    public int ActiveServicesCount { get; set; }
    public int TotalAppointments { get; set; }
    public int BookedAppointmentsCount { get; set; }
    public int CompletedAppointmentsCount { get; set; }
    public int CancelledAppointmentsCount { get; set; }
    public int NoShowAppointmentsCount { get; set; }
    public decimal TotalRevenueCompleted { get; set; }

    public List<AppointmentListItemViewModel> RecentAppointments { get; set; } = new();
    public string TimeZoneDisplayName { get; set; } = string.Empty;
}
