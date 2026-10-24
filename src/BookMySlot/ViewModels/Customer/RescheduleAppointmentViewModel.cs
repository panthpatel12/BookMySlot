using System.ComponentModel.DataAnnotations;

namespace BookMySlot.ViewModels.Customer;

public class RescheduleSlotOption
{
    public int SlotId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string FormattedSchedule { get; set; } = string.Empty;
}

public class RescheduleAppointmentViewModel
{
    public int AppointmentId { get; set; }
    public int CurrentSlotId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string CurrentProviderName { get; set; } = string.Empty;
    public string CurrentSchedule { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a new appointment slot.")]
    [Display(Name = "Select New Available Slot")]
    public int SelectedNewSlotId { get; set; }

    public List<RescheduleSlotOption> AvailableSlots { get; set; } = new();
    public string TimeZoneDisplayName { get; set; } = string.Empty;
}
