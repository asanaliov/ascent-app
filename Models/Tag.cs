using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

public class Tag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string Name { get; set; } = string.Empty;

    public ICollection<TrailTag> TrailTags { get; set; } = new List<TrailTag>();
}
