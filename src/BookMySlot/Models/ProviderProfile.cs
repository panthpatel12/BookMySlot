using System.ComponentModel.DataAnnotations;

namespace BookMySlot.Models;

public class ProviderProfile
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Bio { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<ProviderService> ProviderServices { get; set; } = new List<ProviderService>();
    public ICollection<AppointmentSlot> AppointmentSlots { get; set; } = new List<AppointmentSlot>();
}
