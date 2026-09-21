using Microsoft.AspNetCore.Hosting;

namespace ascent_app.Services;

public interface IImageStorage
{
    Task<(bool Ok, string? Url, string? Error)> SaveAsync(IFormFile file, string subfolder);
    void Delete(string? url);
}

public class ImageStorage : IImageStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> Allowed = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private readonly IWebHostEnvironment _env;

    public ImageStorage(IWebHostEnvironment env) => _env = env;

    public async Task<(bool Ok, string? Url, string? Error)> SaveAsync(IFormFile file, string subfolder)
    {
        if (file.Length == 0) return (false, null, "The file is empty.");
        if (file.Length > MaxBytes) return (false, null, "Image must be 5 MB or smaller.");
        if (!Allowed.TryGetValue(file.ContentType, out var ext))
            return (false, null, "Only JPG, PNG, or WebP images are allowed.");

        var dir = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, name);

        await using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream);

        return (true, $"/uploads/{subfolder}/{name}", null);
    }
    public void Delete(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var root = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
        var path = Path.GetFullPath(Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            File.Delete(path);
    }
}
