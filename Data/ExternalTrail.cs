namespace ascent_app.Data;

public sealed class ExternalTrail
{
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public string Country { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public double DistanceKm { get; set; }
    public int ElevationGainM { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? PhotoUrl { get; set; }
    public string RouteGeoJson { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}
