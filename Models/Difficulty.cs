using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

// seeded lookup, keep bins in sync with DifficultyService
public class Difficulty
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Label { get; set; } = string.Empty;

    public double MinScore { get; set; }
    public double? MaxScore { get; set; } // null = open-ended top bin

    [MaxLength(30)]
    public string BadgeClass { get; set; } = string.Empty; // e.g. "diff-easy"

    public ICollection<Trail> Trails { get; set; } = new List<Trail>();
}
