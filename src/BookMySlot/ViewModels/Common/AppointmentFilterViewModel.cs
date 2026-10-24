using BookMySlot.Models;

namespace BookMySlot.ViewModels.Common;

public class AppointmentFilterViewModel
{
    public AppointmentStatus? Status { get; set; }
    public int? ServiceId { get; set; }
    public int? ProviderId { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class AppointmentListItemViewModel
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public int DurationMinutes { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string FormattedTime { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime BookedAtUtc { get; set; }
    public string FormattedBookedAt { get; set; } = string.Empty;
    public bool CanCancel { get; set; }
    public bool CanReschedule { get; set; }
    public bool CanMarkStatus { get; set; }
}
