using System.ComponentModel.DataAnnotations;

namespace ascent_app.ViewModels;

public class ReviewFormViewModel
{
    public int TrailId { get; set; }

    [Range(1, 5, ErrorMessage = "Pick a rating from 1 to 5.")]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}