using System.ComponentModel.DataAnnotations;

namespace ascent_app.ViewModels;

public class HikeLogFormViewModel
{
    public int TrailId { get; set; }

    // shown on the form, not posted back as the source of truth
    public string? TrailName { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date hiked")]
    public DateTime HikedOn { get; set; } = DateTime.Today;

    [Range(0, 10000)]
    [Display(Name = "Duration (minutes)")]
    public int? DurationMinutes { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? PhotoFile { get; set; }

    [MaxLength(200)]
    [Display(Name = "Photo caption")]
    public string? PhotoCaption { get; set; }
}
