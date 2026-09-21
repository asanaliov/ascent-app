using ascent_app.Data;
using ascent_app.Models;
using ascent_app.Services;
using ascent_app.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Controllers;

public class TrailsController : Controller
{
    private readonly AscentDbContext _context;
    private readonly IDifficultyService _difficulty;
    private readonly IGeoService _geo;
    private readonly ITrailAdviceService _advice;
    private readonly IImageStorage _images;
    private readonly UserManager<ApplicationUser> _userManager;

    public TrailsController(
        AscentDbContext context,
        IDifficultyService difficulty,
        IGeoService geo,
        ITrailAdviceService advice,
        IImageStorage images,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _difficulty = difficulty;
        _geo = geo;
        _advice = advice;
        _images = images;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? q, int? regionId, int? difficultyId, int? tagId, string? sort, double? lat, double? lng, int page = 1)
    {
        const int pageSize = 9;
        var query = _context.Trails
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
            .Include(t => t.Reviews)
            .Include(t => t.Photos)
            .Include(t => t.TrailTags)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t =>
                EF.Functions.Like(t.Name, $"%{term}%") ||
                EF.Functions.Like(t.ShortDescription, $"%{term}%"));
        }

        if (regionId.HasValue)
            query = query.Where(t => t.RegionId == regionId);

        if (difficultyId.HasValue)
            query = query.Where(t => t.DifficultyId == difficultyId);

        if (tagId.HasValue)
            query = query.Where(t => t.TrailTags.Any(tt => tt.TagId == tagId));

        var trails = await query.ToListAsync();

        var liveLocation = lat.HasValue && lng.HasValue;
        double? effLat = lat, effLng = lng;
        string? homeName = null;
        if (!liveLocation && User.Identity?.IsAuthenticated == true)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me?.HomeLat is double hLat && me.HomeLng is double hLng)
            {
                effLat = hLat;
                effLng = hLng;
                homeName = me.HomeLocationName;
            }
        }

        Dictionary<int, double>? distances = null;
        if (effLat.HasValue && effLng.HasValue)
            distances = trails.ToDictionary(
                t => t.Id, t => _geo.DistanceKm(effLat.Value, effLng.Value, t.Latitude, t.Longitude));

        sort = string.IsNullOrWhiteSpace(sort) ? (distances != null ? "nearest" : "newest") : sort;
        trails = sort switch
        {
            "nearest" when distances != null => trails.OrderBy(t => distances[t.Id]).ToList(),
            "name" => trails.OrderBy(t => t.Name).ToList(),
            "distance" => trails.OrderBy(t => t.DistanceKm).ToList(),
            "elevation" => trails.OrderByDescending(t => t.ElevationGainM).ToList(),
            "rating" => trails.OrderByDescending(t => t.AverageRating).ToList(),
            _ => trails.OrderByDescending(t => t.CreatedAt).ToList(),
        };

        ViewBag.MapTrails = System.Text.Json.JsonSerializer.Serialize(trails.Select(t => new
        {
            name = t.Name,
            lat = t.Latitude,
            lng = t.Longitude,
            route = t.RouteGeoJson,
            diff = t.Difficulty?.Label ?? "",
            url = Url.Action("Details", new { id = t.Id }),
        }));

        var totalCount = trails.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;
        trails = trails.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var regions = await _context.Regions
            .OrderBy(r => r.Country).ThenBy(r => r.Name).AsNoTracking().ToListAsync();
        var difficulties = await _context.Difficulties.AsNoTracking().ToListAsync();
        ViewBag.Regions = new SelectList(
            regions.Select(r => new { r.Id, Label = $"{r.Name} ({r.Country})" }), "Id", "Label", regionId);
        ViewBag.Difficulties = new SelectList(difficulties, "Id", "Label", difficultyId);
        ViewBag.Tags = await _context.Tags.OrderBy(t => t.Name).AsNoTracking().ToListAsync();
        ViewBag.Filter = new TrailFilterViewModel
        {
            Q = q,
            RegionId = regionId,
            DifficultyId = difficultyId,
            TagId = tagId,
        };
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.UserLat = effLat;
        ViewBag.UserLng = effLng;
        ViewBag.IsLiveLocation = liveLocation;
        ViewBag.HomeName = homeName;
        ViewBag.FocusLat = effLat ?? 41.6;
        ViewBag.FocusLng = effLng ?? 21.7;
        ViewBag.FocusZoom = effLat.HasValue ? 8.5 : 6.5;
        ViewBag.Distances = distances?.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 1));

        ViewBag.FavoriteTrailIds = await GetFavoriteTrailIdsAsync();
        return View(trails);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var trail = await _context.Trails
            .Include(t => t.Author)
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
            .Include(t => t.Reviews).ThenInclude(r => r.User)
            .Include(t => t.TrailTags).ThenInclude(tt => tt.Tag)
            .Include(t => t.Photos)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (trail == null) return NotFound();

        ViewBag.FavoriteTrailIds = await GetFavoriteTrailIdsAsync();
        ViewBag.Advice = _advice.For(trail);
        return View(trail);
    }

    public async Task<IActionResult> Nearby(double? lat, double? lng)
    {
        var trails = await _context.Trails
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
            .Include(t => t.Photos)
            .AsNoTracking()
            .ToListAsync();

        var vm = new NearbyViewModel { Lat = lat, Lng = lng };

        if (lat.HasValue && lng.HasValue)
        {
            vm.Trails = _geo.NearestTo(lat.Value, lng.Value, trails)
                .Select(x => new NearbyTrail { Trail = x.Trail, DistanceKm = x.DistanceKm })
                .ToList();
        }
        else
        {
            vm.Trails = trails
                .OrderBy(t => t.Name)
                .Select(t => new NearbyTrail { Trail = t })
                .ToList();
        }

        return View(vm);
    }

    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateRegionsAsync();
        await PopulateTagsAsync();
        return View(new TrailFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Create(TrailFormViewModel form)
    {
        NormalizeRoute(form);

        if (!ModelState.IsValid)
        {
            await PopulateRegionsAsync(form.RegionId);
            await PopulateTagsAsync();
            return View(form);
        }

        var trail = new Trail
        {
            Name = form.Name,
            ShortDescription = form.ShortDescription,
            Description = form.Description,
            DistanceKm = form.DistanceKm,
            ElevationGainM = form.ElevationGainM,
            Latitude = form.Latitude,
            Longitude = form.Longitude,
            RouteGeoJson = form.RouteGeoJson,
            RegionId = form.RegionId,
            DifficultyId = await _difficulty.ResolveDifficultyIdAsync(form.DistanceKm, form.ElevationGainM),
            AuthorId = _userManager.GetUserId(User),
            CreatedAt = DateTime.UtcNow,
            TrailTags = form.SelectedTagIds.Select(tid => new TrailTag { TagId = tid }).ToList(),
        };

        _context.Add(trail);
        await _context.SaveChangesAsync();
        await AddPhotosAsync(trail.Id, form.PhotoFiles);
        return RedirectToAction(nameof(Details), new { id = trail.Id });
    }

    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var trail = await _context.Trails
            .Include(t => t.TrailTags)
            .Include(t => t.Photos)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
        if (trail == null) return NotFound();

        await PopulateRegionsAsync(trail.RegionId);
        await PopulateTagsAsync();

        var form = ToForm(trail);
        form.SelectedTagIds = trail.TrailTags.Select(tt => tt.TagId).ToList();
        form.ExistingPhotos = trail.Photos.OrderByDescending(p => p.IsCoverImage).ThenBy(p => p.Id).ToList();
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Edit(int id, TrailFormViewModel form)
    {
        if (id != form.Id) return NotFound();

        NormalizeRoute(form);

        if (!ModelState.IsValid)
        {
            await PopulateRegionsAsync(form.RegionId);
            await PopulateTagsAsync();
            form.ExistingPhotos = await _context.TrailPhotos.AsNoTracking()
                .Where(p => p.TrailId == id)
                .OrderByDescending(p => p.IsCoverImage).ThenBy(p => p.Id)
                .ToListAsync();
            return View(form);
        }

        var trail = await _context.Trails
            .Include(t => t.TrailTags)
            .Include(t => t.Photos)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (trail == null) return NotFound();

        trail.Name = form.Name;
        trail.ShortDescription = form.ShortDescription;
        trail.Description = form.Description;
        trail.DistanceKm = form.DistanceKm;
        trail.ElevationGainM = form.ElevationGainM;
        trail.Latitude = form.Latitude;
        trail.Longitude = form.Longitude;
        trail.RouteGeoJson = form.RouteGeoJson;
        trail.RegionId = form.RegionId;
        trail.DifficultyId = await _difficulty.ResolveDifficultyIdAsync(form.DistanceKm, form.ElevationGainM);

        trail.TrailTags.Clear();
        foreach (var tid in form.SelectedTagIds)
            trail.TrailTags.Add(new TrailTag { TagId = tid });

        foreach (var photo in trail.Photos.Where(p => form.RemovePhotoIds.Contains(p.Id)).ToList())
            trail.Photos.Remove(photo);
        if (form.CoverPhotoId is int coverId && trail.Photos.Any(p => p.Id == coverId))
            foreach (var photo in trail.Photos)
                photo.IsCoverImage = photo.Id == coverId;
        if (trail.Photos.Count > 0 && !trail.Photos.Any(p => p.IsCoverImage))
            trail.Photos.OrderBy(p => p.Id).First().IsCoverImage = true;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TrailExists(form.Id)) return NotFound();
            throw;
        }

        await AddPhotosAsync(trail.Id, form.PhotoFiles);
        return RedirectToAction(nameof(Details), new { id = trail.Id });
    }

    // Saves each uploaded file and attaches it; the first photo a trail gets becomes its cover.
    private async Task AddPhotosAsync(int trailId, List<IFormFile>? files)
    {
        var errors = new List<string>();
        foreach (var file in (files ?? new()).Where(f => f.Length > 0))
        {
            var (ok, url, error) = await _images.SaveAsync(file, "trails");
            if (!ok)
            {
                errors.Add($"{file.FileName}: {error}");
                continue;
            }

            var hasPhotos = await _context.TrailPhotos.AnyAsync(p => p.TrailId == trailId);
            _context.TrailPhotos.Add(new TrailPhoto { TrailId = trailId, Url = url!, IsCoverImage = !hasPhotos });
            await _context.SaveChangesAsync();
        }

        if (errors.Count > 0)
            TempData["TrailPhotoError"] = string.Join(" ", errors);
    }

    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var trail = await _context.Trails
            .Include(t => t.Author)
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (trail == null) return NotFound();

        return View(trail);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var trail = await _context.Trails.FindAsync(id);
        if (trail != null)
        {
            _context.Trails.Remove(trail);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool TrailExists(int id) => _context.Trails.Any(e => e.Id == id);

    private void NormalizeRoute(TrailFormViewModel form)
    {
        if (string.IsNullOrWhiteSpace(form.RouteGeoJson))
        {
            form.RouteGeoJson = null;
            return;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(form.RouteGeoJson);
            var root = doc.RootElement;
            var isLine = root.TryGetProperty("type", out var type)
                         && type.GetString() == "LineString"
                         && root.TryGetProperty("coordinates", out var coords)
                         && coords.ValueKind == System.Text.Json.JsonValueKind.Array
                         && coords.GetArrayLength() >= 2;
            if (!isLine)
                ModelState.AddModelError(nameof(form.RouteGeoJson), "Draw at least two points to define a route.");
        }
        catch (System.Text.Json.JsonException)
        {
            ModelState.AddModelError(nameof(form.RouteGeoJson), "The route data is not valid.");
        }
    }

    private async Task<HashSet<int>> GetFavoriteTrailIdsAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return new HashSet<int>();

        return (await _context.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.TrailId)
            .ToListAsync()).ToHashSet();
    }

    private async Task PopulateRegionsAsync(int? selected = null)
    {
        var regions = await _context.Regions.OrderBy(r => r.Name).AsNoTracking().ToListAsync();
        ViewBag.RegionId = new SelectList(regions, "Id", "Name", selected);
    }

    private async Task PopulateTagsAsync()
    {
        ViewBag.AllTags = await _context.Tags.OrderBy(t => t.Name).AsNoTracking().ToListAsync();
        // the form previews the difficulty label live, using the same bins the service resolves against
        ViewBag.DifficultyBins = await _context.Difficulties
            .OrderBy(d => d.MinScore)
            .Select(d => new { d.Label, d.MinScore, d.BadgeClass })
            .AsNoTracking()
            .ToListAsync();
    }

    private static TrailFormViewModel ToForm(Trail t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        ShortDescription = t.ShortDescription,
        Description = t.Description,
        DistanceKm = t.DistanceKm,
        ElevationGainM = t.ElevationGainM,
        Latitude = t.Latitude,
        Longitude = t.Longitude,
        RouteGeoJson = t.RouteGeoJson,
        RegionId = t.RegionId,
    };
}
