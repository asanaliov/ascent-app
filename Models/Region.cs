using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

public class Region
{
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<Trail> Trails { get; set; } = new List<Trail>();
}
