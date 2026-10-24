namespace BookMySlot.Models;

public class ProviderService
{
    public int ProviderProfileId { get; set; }
    public ProviderProfile ProviderProfile { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
