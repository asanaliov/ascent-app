using System.Text.RegularExpressions;
using ascent_app.Models;

namespace ascent_app.Services;

public interface ITrailPhotoCache
{
    Task<string?> CacheAsync(Trail trail, string? sourceUrl, CancellationToken cancellationToken = default);
}

public sealed class LocalTrailPhotoCache : ITrailPhotoCache
{
    private const int MaxBytes = 12 * 1024 * 1024;
    private static readonly Regex SlugPattern = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly HttpClient _http;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalTrailPhotoCache> _logger;

    public LocalTrailPhotoCache(HttpClient http, IWebHostEnvironment env, ILogger<LocalTrailPhotoCache> logger)
    {
        _http = http;
        _env = env;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("AscentApp/1.0 (trail photo cache; local images)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("image/*,*/*;q=0.8");
    }

    public async Task<string?> CacheAsync(Trail trail, string? sourceUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            return null;

        if (IsLocal(sourceUrl))
            return sourceUrl;

        var fileName = $"{Slugify(trail.Name)}-{trail.Id}{GetExtension(sourceUrl)}";
        var relativePath = $"/img/trails/{fileName}";
        var dir = Path.Combine(_env.WebRootPath, "img", "trails");
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, fileName);
        if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 0)
            return relativePath;

        var tempPath = fullPath + ".tmp";
        try
        {
            using var response = await _http.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is long bytes && bytes > MaxBytes)
                throw new InvalidOperationException("Image is too large.");

            var ext = string.IsNullOrWhiteSpace(Path.GetExtension(fullPath))
                ? ExtensionFromContentType(response.Content.Headers.ContentType?.MediaType)
                : Path.GetExtension(fullPath);

            if (!string.IsNullOrWhiteSpace(ext) && !fullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                fullPath = Path.Combine(dir, $"{Slugify(trail.Name)}-{trail.Id}{ext}");
                relativePath = $"/img/trails/{Path.GetFileName(fullPath)}";
                tempPath = fullPath + ".tmp";
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using (var target = File.Create(tempPath))
                await source.CopyToAsync(target, cancellationToken);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            File.Move(tempPath, fullPath);
            return relativePath;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or IOException)
        {
            _logger.LogWarning(ex, "Could not cache image for trail {Trail}.", trail.Name);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            return null;
        }
    }

    private static bool IsLocal(string url)
        => url.StartsWith("/img/trails/", StringComparison.OrdinalIgnoreCase)
           || url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
           || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static string Slugify(string value)
    {
        var slug = SlugPattern.Replace(value.ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "trail" : slug;
    }

    private static string GetExtension(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(ext))
                return ext.ToLowerInvariant();
        }

        return ".jpg";
    }

    private static string ExtensionFromContentType(string? mediaType)
        => mediaType?.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            _ => ".jpg",
        };
}
