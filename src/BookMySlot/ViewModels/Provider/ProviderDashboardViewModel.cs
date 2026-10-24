using BookMySlot.ViewModels.Common;

namespace BookMySlot.ViewModels.Provider;

public class ProviderDashboardViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int TodayAppointmentsCount { get; set; }
    public int UpcomingAppointmentsCount { get; set; }
    public int CompletedAppointmentsCount { get; set; }
    public int ActiveSlotsCount { get; set; }
    public List<AppointmentListItemViewModel> TodayAppointments { get; set; } = new();
    public List<AppointmentListItemViewModel> UpcomingAppointments { get; set; } = new();
    public string TimeZoneDisplayName { get; set; } = string.Empty;
}
