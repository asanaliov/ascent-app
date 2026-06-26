using System.ComponentModel.DataAnnotations;

namespace ascent_app.ViewModels;

public class EditProfileViewModel
{
    [Required]
    [MaxLength(80)]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(100)]
    [Display(Name = "Home location")]
    public string? HomeLocationName { get; set; }

    [Range(-90, 90)]
    public double? HomeLat { get; set; }

    [Range(-180, 180)]
    public double? HomeLng { get; set; }

    public string? Email { get; set; }
    public DateTime MemberSince { get; set; }
    public string? UserId { get; set; }
}
