using System.ComponentModel.DataAnnotations;

namespace BookMySlot.ViewModels.Customer;

public class BookAppointmentViewModel
{
    [Required]
    public int SlotId { get; set; }

    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceDescription { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }

    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderBio { get; set; } = string.Empty;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string FormattedSchedule { get; set; } = string.Empty;
    public string TimeZoneDisplayName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes / Reason for Appointment (Optional)")]
    public string? Notes { get; set; }
}
