using System.ComponentModel.DataAnnotations;
using ascent_app.Models;

namespace ascent_app.Areas.Admin.ViewModels;

public class RegionFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class TagFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string Name { get; set; } = string.Empty;
}

public class BadgeFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    [MaxLength(40)]
    [Display(Name = "Icon (tabler class, e.g. ti-mountain)")]
    public string? IconName { get; set; }

    public BadgeCriteria Criteria { get; set; }

    [Range(1, 1_000_000)]
    public int Threshold { get; set; } = 1;
}

public class UserRowViewModel
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public bool IsGuide { get; set; }
    public bool IsAdmin { get; set; }
}
