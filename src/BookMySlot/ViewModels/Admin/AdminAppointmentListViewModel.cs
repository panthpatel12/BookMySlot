using BookMySlot.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookMySlot.ViewModels.Admin;

public class AdminAppointmentListViewModel
{
    public AppointmentFilterViewModel Filter { get; set; } = new();
    public List<AppointmentListItemViewModel> Appointments { get; set; } = new();
    public List<SelectListItem> ServicesList { get; set; } = new();
    public List<SelectListItem> ProvidersList { get; set; } = new();
    public string TimeZoneDisplayName { get; set; } = string.Empty;
}
