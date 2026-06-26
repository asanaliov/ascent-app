using System.Text.Json;
using ascent_app.Models;

namespace ascent_app.Services;

public interface ITrailPhotoSource
{
    Task<string?> FindPhotoAsync(Trail trail, CancellationToken cancellationToken = default);
}

public sealed class OpenverseTrailPhotoSource : ITrailPhotoSource
{
    private const string Endpoint = "https://api.openverse.engineering/v1/images/";
    private readonly HttpClient _http;
    private readonly ILogger<OpenverseTrailPhotoSource> _logger;

    public OpenverseTrailPhotoSource(HttpClient http, ILogger<OpenverseTrailPhotoSource> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string?> FindPhotoAsync(Trail trail, CancellationToken cancellationToken = default)
    {
        foreach (var query in Queries(trail))
        {
            var url = await SearchAsync(query, cancellationToken);
            if (!string.IsNullOrWhiteSpace(url)) return url;
        }

        return null;
    }

    private async Task<string?> SearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{Endpoint}?q={Uri.EscapeDataString(query)}&page_size=1&format=json";
            await using var stream = await _http.GetStreamAsync(url, cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array)
                return null;

            var first = results.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined) return null;

            if (first.TryGetProperty("url", out var imageUrl) &&
                imageUrl.ValueKind == JsonValueKind.String)
                return imageUrl.GetString();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Could not find image for query {Query}.", query);
        }

        return null;
    }

    private static IEnumerable<string> Queries(Trail trail)
    {
        yield return $"{trail.Name} {trail.Region?.Country}";
        yield return $"{trail.Region?.Name} {trail.Region?.Country} mountain";
        yield return $"{trail.Region?.Name} hiking trail";
    }
}
