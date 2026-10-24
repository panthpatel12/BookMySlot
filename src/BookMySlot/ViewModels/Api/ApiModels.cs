namespace BookMySlot.ViewModels.Api;

public class ServiceApiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "INR";
}

public class AvailableSlotApiDto
{
    public int SlotId { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string StartLocal { get; set; } = string.Empty;
    public string EndLocal { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
}

public class AvailableSlotsApiResponse
{
    public string Date { get; set; } = string.Empty;
    public string InterpretedAsTimeZone { get; set; } = string.Empty;
    public int TotalAvailableSlots { get; set; }
    public List<AvailableSlotApiDto> Slots { get; set; } = new();
}
