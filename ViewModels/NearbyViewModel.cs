using ascent_app.Models;

namespace ascent_app.ViewModels;

public class NearbyViewModel
{
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool HasLocation => Lat.HasValue && Lng.HasValue;

    public List<NearbyTrail> Trails { get; set; } = new();
}

public class NearbyTrail
{
    public Trail Trail { get; set; } = null!;
    public double? DistanceKm { get; set; }
}
