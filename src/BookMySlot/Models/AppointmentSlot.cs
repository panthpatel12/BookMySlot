using System.ComponentModel.DataAnnotations;

namespace BookMySlot.Models;

public class AppointmentSlot
{
    public int Id { get; set; }

    public int ProviderProfileId { get; set; }
    public ProviderProfile ProviderProfile { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    [Required]
    public DateTime StartUtc { get; set; }

    [Required]
    public DateTime EndUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public SlotReservation? SlotReservation { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
