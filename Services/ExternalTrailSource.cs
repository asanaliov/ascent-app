using System.Globalization;
using System.Text.Json;
using ascent_app.Data;

namespace ascent_app.Services;

public interface IExternalTrailSource
{
    Task<IReadOnlyList<ExternalTrail>> GetTrailsAsync(CancellationToken cancellationToken = default);
}

public sealed class OverpassTrailSource : IExternalTrailSource
{
    private const string DefaultEndpoint = "https://overpass-api.de/api/interpreter";
    private static readonly string[] FallbackEndpoints = { "https://overpass.kumi.systems/api/interpreter" };
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OverpassTrailSource> _logger;

    public OverpassTrailSource(HttpClient http, IConfiguration config, ILogger<OverpassTrailSource> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("AscentApp/1.0 (trail import; local development)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<IReadOnlyList<ExternalTrail>> GetTrailsAsync(CancellationToken cancellationToken = default)
    {
        var countryIso = _config["TrailData:CountryIso"] ?? "MK";
        var limit = int.TryParse(_config["TrailData:Limit"], out var parsedLimit) ? parsedLimit : 16;

        foreach (var endpoint in Endpoints())
        {
            try
            {
                var trails = await GetTrailsFromEndpointAsync(endpoint, countryIso, limit, cancellationToken);
                if (trails.Count > 0) return trails;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "Could not import trails from Overpass endpoint {Endpoint}.", endpoint);
            }
        }

        return Array.Empty<ExternalTrail>();
    }

    private async Task<IReadOnlyList<ExternalTrail>> GetTrailsFromEndpointAsync(
        string endpoint,
        string countryIso,
        int limit,
        CancellationToken cancellationToken)
    {
        var queryLimit = Math.Max(limit * 3, limit);
        var query = $"""
            [out:json][timeout:50];
            area["ISO3166-1"="{countryIso}"][admin_level=2]->.country;
            (
              relation["route"="hiking"]["name"](area.country);
              relation["route"="foot"]["name"](area.country);
            );
            out geom {queryLimit};
            """;

        using var response = await _http.PostAsync(
            endpoint,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = query }),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("elements", out var elements) ||
            elements.ValueKind != JsonValueKind.Array)
            return Array.Empty<ExternalTrail>();

        return elements
            .EnumerateArray()
            .Select(ToTrail)
            .Where(t => t != null)
            .Select(t => t!)
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(limit)
            .ToList();
    }

    private IEnumerable<string> Endpoints()
        => new[] { _config["TrailData:OverpassEndpoint"] ?? DefaultEndpoint }
            .Concat(FallbackEndpoints)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static ExternalTrail? ToTrail(JsonElement element)
    {
        if (!element.TryGetProperty("tags", out var tags)) return null;
        if (!TryGet(tags, "name", out var name) || string.IsNullOrWhiteSpace(name)) return null;

        var coords = ReadCoordinates(element);
        if (coords.Count < 2) return null;

        var distanceKm = TryReadDistance(tags, out var taggedDistance)
            ? taggedDistance
            : DistanceKm(coords);
        if (distanceKm <= 0) return null;

        var gainM = TryReadIntTag(tags, "ascent", out var ascent) ? ascent : 0;
        var region = FirstTag(tags, "addr:city", "addr:municipality", "addr:district", "operator") ?? "North Macedonia";
        var summary = FirstTag(tags, "description", "note")
            ?? $"{name} is a mapped hiking route from OpenStreetMap.";

        return new ExternalTrail
        {
            Name = name.Trim(),
            Region = region.Trim(),
            Country = "North Macedonia",
            ShortDescription = Trim(summary, 200),
            DistanceKm = Math.Round(distanceKm, 1),
            ElevationGainM = Math.Max(0, gainM),
            Longitude = coords[0][0],
            Latitude = coords[0][1],
            PhotoUrl = ReadPhotoUrl(tags),
            RouteGeoJson = JsonSerializer.Serialize(new
            {
                type = "LineString",
                coordinates = coords,
            }),
            Tags = BuildTags(tags),
        };
    }

    private static List<double[]> ReadCoordinates(JsonElement element)
    {
        var coords = new List<double[]>();
        if (!element.TryGetProperty("members", out var members) ||
            members.ValueKind != JsonValueKind.Array)
            return coords;

        foreach (var member in members.EnumerateArray())
        {
            if (!member.TryGetProperty("geometry", out var geometry) ||
                geometry.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var point in geometry.EnumerateArray())
            {
                if (!point.TryGetProperty("lon", out var lon) ||
                    !point.TryGetProperty("lat", out var lat))
                    continue;

                var next = new[] { lon.GetDouble(), lat.GetDouble() };
                if (coords.Count == 0 || !SamePoint(coords[^1], next))
                    coords.Add(next);
            }
        }

        return coords;
    }

    private static bool SamePoint(double[] a, double[] b)
        => Math.Abs(a[0] - b[0]) < 0.000001 && Math.Abs(a[1] - b[1]) < 0.000001;

    private static bool TryGet(JsonElement tags, string name, out string value)
    {
        if (tags.TryGetProperty(name, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString() ?? "";
            return true;
        }

        value = "";
        return false;
    }

    private static string? FirstTag(JsonElement tags, params string[] names)
    {
        foreach (var name in names)
            if (TryGet(tags, name, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

        return null;
    }

    private static bool TryReadDistance(JsonElement tags, out double value)
    {
        value = 0;
        if (!TryGet(tags, "distance", out var raw)) return false;

        raw = raw.ToLowerInvariant()
            .Replace("km", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", ".", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadIntTag(JsonElement tags, string name, out int value)
    {
        value = 0;
        return TryGet(tags, name, out var raw) &&
               int.TryParse(new string(raw.Where(char.IsDigit).ToArray()), out value);
    }

    private static double DistanceKm(List<double[]> coords)
    {
        var total = 0.0;
        for (var i = 1; i < coords.Count; i++)
            total += SegmentKm(coords[i - 1], coords[i]);
        return total;
    }

    private static double SegmentKm(double[] a, double[] b)
    {
        const double r = 6371.0;
        var dLat = ToRad(b[1] - a[1]);
        var dLon = ToRad(b[0] - a[0]);
        var lat1 = ToRad(a[1]);
        var lat2 = ToRad(b[1]);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;

    private static List<string> BuildTags(JsonElement tags)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (TryGet(tags, "network", out var network) && !string.IsNullOrWhiteSpace(network))
            result.Add(network.ToUpperInvariant());
        if (TryGet(tags, "sac_scale", out var sacScale) && !string.IsNullOrWhiteSpace(sacScale))
            result.Add(sacScale.Replace("_", " "));
        if (TryGet(tags, "roundtrip", out var roundtrip) && roundtrip.Equals("yes", StringComparison.OrdinalIgnoreCase))
            result.Add("Loop");
        if (TryGet(tags, "route", out var route) && !string.IsNullOrWhiteSpace(route))
            result.Add(route);

        result.Add("OpenStreetMap");
        return result.Take(6).ToList();
    }

    private static string? ReadPhotoUrl(JsonElement tags)
    {
        var image = FirstTag(tags, "image", "wikimedia", "wikimedia_commons");
        if (string.IsNullOrWhiteSpace(image)) return null;

        image = image.Trim();
        if (image.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            image.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return image;

        if (image.StartsWith("File:", StringComparison.OrdinalIgnoreCase))
            return "https://commons.wikimedia.org/wiki/Special:FilePath/" + Uri.EscapeDataString(image);

        if (image.StartsWith("Category:", StringComparison.OrdinalIgnoreCase))
            return null;

        return "https://commons.wikimedia.org/wiki/Special:FilePath/" + Uri.EscapeDataString("File:" + image);
    }

    private static string Trim(string value, int max)
        => value.Length <= max ? value : value[..max].TrimEnd() + "...";
}
