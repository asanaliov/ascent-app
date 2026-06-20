using System.ComponentModel.DataAnnotations;

namespace ascent_app.ViewModels;

// Form model for create/edit. Difficulty is computed from distance + gain, and
// the author is the current user - neither is on the form.
public class TrailFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Display(Name = "Short description")]
    public string ShortDescription { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(0.1, 1000)]
    [Display(Name = "Distance (km)")]
    public double DistanceKm { get; set; }

    [Range(0, 10000)]
    [Display(Name = "Elevation gain (m)")]
    public int ElevationGainM { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [MaxLength(400)]
    [Display(Name = "Photo URL")]
    public string? PhotoUrl { get; set; }

    [Required]
    [Display(Name = "Region")]
    public int RegionId { get; set; }

    public List<int> SelectedTagIds { get; set; } = new();
}
