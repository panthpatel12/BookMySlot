using System.ComponentModel.DataAnnotations;

namespace BookMySlot.ViewModels.Admin;

public class ServiceAssignmentItem
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsAssigned { get; set; }
}

public class ProviderAssignmentViewModel
{
    public int ProviderProfileId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Bio / Qualifications")]
    public string? Bio { get; set; }

    [Display(Name = "Provider Status (Active/Inactive)")]
    public bool IsActive { get; set; }

    public List<ServiceAssignmentItem> Services { get; set; } = new();
    public List<int> SelectedServiceIds { get; set; } = new();
}
