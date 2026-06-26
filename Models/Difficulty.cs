using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

public class Difficulty
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Label { get; set; } = string.Empty;

    public double MinScore { get; set; }
    public double? MaxScore { get; set; }

    [MaxLength(30)]
    public string BadgeClass { get; set; } = string.Empty;

    public ICollection<Trail> Trails { get; set; } = new List<Trail>();
}
