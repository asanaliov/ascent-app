using ascent_app.Models;
using ascent_app.Services;
using Xunit;

namespace Ascent.Tests;

public class GeoServiceTests
{
    private static GeoService Sut() => new GeoService();

    [Fact]
    public void DistanceKm_SameCoords_IsZero()
    {
        var d = Sut().DistanceKm(41.99, 21.43, 41.99, 21.43);
        Assert.Equal(0.0, d, 6);
    }

    [Fact]
    public void DistanceKm_OneDegreeLatitude_IsAboutOneEleven()
    {
        // ~111 km per degree of latitude
        var d = Sut().DistanceKm(0, 0, 1, 0);
        Assert.InRange(d, 109.0, 113.0);
    }

    [Fact]
    public void NearestTo_OrdersAscendingByDistance_ClosestFirst()
    {
        var near = new Trail { Name = "Near", Latitude = 0.1, Longitude = 0.0 };
        var mid = new Trail { Name = "Mid", Latitude = 1.0, Longitude = 0.0 };
        var far = new Trail { Name = "Far", Latitude = 5.0, Longitude = 0.0 };

        // feed out of order
        var result = Sut().NearestTo(0, 0, new[] { far, near, mid });

        Assert.Equal(3, result.Count);
        Assert.Equal("Near", result[0].Trail.Name);

        // distances are non-decreasing
        for (int i = 1; i < result.Count; i++)
            Assert.True(result[i].DistanceKm >= result[i - 1].DistanceKm);
    }
}
