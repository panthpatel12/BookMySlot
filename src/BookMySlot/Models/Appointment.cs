using System.ComponentModel.DataAnnotations;

namespace BookMySlot.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser Customer { get; set; } = null!;

    public int AppointmentSlotId { get; set; }
    public AppointmentSlot AppointmentSlot { get; set; } = null!;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime BookedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAtUtc { get; set; }

    // Navigation properties
    public SlotReservation? SlotReservation { get; set; }
}
