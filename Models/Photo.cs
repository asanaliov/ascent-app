using System.ComponentModel.DataAnnotations;

namespace ascent_app.Models;

// hike-log photos. part of the Trail/HikeLog/Photo cascade cycle - restrict in OnModelCreating
public class Photo
{
    public int Id { get; set; }

    [Required]
    [MaxLength(400)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Caption { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int HikeLogId { get; set; }
    public HikeLog HikeLog { get; set; } = null!;
}
