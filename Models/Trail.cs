using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ascent_app.Models;

public class Trail
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? SeedKey { get; set; }

    [Required]
    [MaxLength(200)]
    public string ShortDescription { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(0, 1000)]
    public double DistanceKm { get; set; }

    [Range(0, 10000)]
    public int ElevationGainM { get; set; }

    [Range(0, 168)]
    public double EstimatedTimeHours { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [MaxLength(400)]
    public string? PhotoUrl { get; set; }

    [MaxLength(100)]
    public string Source { get; set; } = "Local";

    public bool IsSeedData { get; set; }

    public string? RouteGeoJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RegionId { get; set; }
    public Region Region { get; set; } = null!;

    public int DifficultyId { get; set; }
    public Difficulty Difficulty { get; set; } = null!;

    public string? AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }

    public ICollection<TrailPhoto> Photos { get; set; } = new List<TrailPhoto>();
    public ICollection<TrailTag> TrailTags { get; set; } = new List<TrailTag>();
    public ICollection<HikeLog> HikeLogs { get; set; } = new List<HikeLog>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<HikeEvent> HikeEvents { get; set; } = new List<HikeEvent>();

    [NotMapped]
    public double AverageRating =>
        Reviews.Count == 0 ? 0 : Math.Round(Reviews.Average(r => r.Rating), 1);

    [NotMapped]
    public string CoverImageUrl =>
        Photos.FirstOrDefault(p => p.IsCoverImage)?.Url
        ?? Photos.FirstOrDefault()?.Url
        ?? PhotoUrl
        ?? "/images/trails/default-trail.svg";
}
