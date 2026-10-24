namespace BookMySlot.ViewModels.Provider;

public class SlotDisplayItemViewModel
{
    public int SlotId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string FormattedTime { get; set; } = string.Empty;
    public bool IsBooked { get; set; }
    public bool IsActive { get; set; }
    public int? AppointmentId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Status { get; set; }
    public bool CanRemoveSlot { get; set; }
    public bool CanMarkStatus { get; set; }
}

public class ProviderScheduleViewModel
{
    public DateTime SelectedDate { get; set; }
    public string FormattedDate { get; set; } = string.Empty;
    public string TimeZoneDisplayName { get; set; } = string.Empty;
    public List<SlotDisplayItemViewModel> Slots { get; set; } = new();
}
