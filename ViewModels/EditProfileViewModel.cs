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

    // read-only carry-through fields, not edited
    public string? Email { get; set; }
    public DateTime MemberSince { get; set; }
    public string? UserId { get; set; }
}
