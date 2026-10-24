using BookMySlot.Models;

namespace BookMySlot.ViewModels.Customer;

public class AppointmentDetailsViewModel
{
    public int AppointmentId { get; set; }
    public AppointmentStatus Status { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceDescription { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }

    public string ProviderName { get; set; } = string.Empty;
    public string ProviderEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string FormattedSchedule { get; set; } = string.Empty;
    public string TimeZoneDisplayName { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public DateTime BookedAtUtc { get; set; }
    public string FormattedBookedAt { get; set; } = string.Empty;
    public DateTime? CancelledAtUtc { get; set; }
    public string? FormattedCancelledAt { get; set; }

    public bool CanCancel { get; set; }
    public bool CanReschedule { get; set; }
}
