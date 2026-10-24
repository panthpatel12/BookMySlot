using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookMySlot.ViewModels.Provider;

public class CreateSlotViewModel
{
    [Required(ErrorMessage = "Please select a service.")]
    [Display(Name = "Service")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Start Date & Time is required.")]
    [Display(Name = "Start Time (Local Application Time)")]
    public DateTime LocalStartTime { get; set; } = DateTime.Today.AddDays(1).AddHours(10); // default tomorrow 10:00 AM

    public string TimeZoneDisplayName { get; set; } = string.Empty;

    public List<SelectListItem> AssignedServices { get; set; } = new();
}
