using ascent_app.Models;

namespace ascent_app.Services;

public interface IGeoService
{
    double DistanceKm(double lat1, double lon1, double lat2, double lon2);
    IReadOnlyList<(Trail Trail, double DistanceKm)> NearestTo(double lat, double lon, IEnumerable<Trail> trails);
}

public class GeoService : IGeoService
{
    public double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        double dLat = Deg2Rad(lat2 - lat1), dLon = Deg2Rad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2))
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public IReadOnlyList<(Trail Trail, double DistanceKm)> NearestTo(
        double lat, double lon, IEnumerable<Trail> trails)
        => trails
            .Select(t => (t, Math.Round(DistanceKm(lat, lon, t.Latitude, t.Longitude), 1)))
            .OrderBy(x => x.Item2)
            .ToList();

    private static double Deg2Rad(double d) => d * Math.PI / 180.0;
}
