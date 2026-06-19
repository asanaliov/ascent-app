using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

public class TrailPhoto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(400)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Caption { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int TrailId { get; set; }
    public Trail Trail { get; set; } = null!;
}
