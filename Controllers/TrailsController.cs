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
    private readonly UserManager<ApplicationUser> _userManager;

    public TrailsController(
        AscentDbContext context,
        IDifficultyService difficulty,
        IGeoService geo,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _difficulty = difficulty;
        _geo = geo;
        _userManager = userManager;
    }

    // GET: Trails?q=&regionId=&difficultyId=&tagId=&sort=&lat=&lng=&page=
    public async Task<IActionResult> Index(string? q, int? regionId, int? difficultyId, int? tagId, string? sort, double? lat, double? lng, int page = 1)
    {
        const int pageSize = 9;
        var query = _context.Trails
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
            .Include(t => t.Reviews)
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

        // distance from the visitor's location, when the browser shared it (global app:
        // the catalogue spans continents, so "nearest to me" is the natural default view)
        Dictionary<int, double>? distances = null;
        if (lat.HasValue && lng.HasValue)
            distances = trails.ToDictionary(
                t => t.Id, t => _geo.DistanceKm(lat.Value, lng.Value, t.Latitude, t.Longitude));

        // sort in-memory (AverageRating is [NotMapped]); default to nearest when located
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

        // overview-map data for the whole filtered set (before paging, so the map
        // shows every match, not just this page's 9)
        ViewBag.MapTrails = System.Text.Json.JsonSerializer.Serialize(trails.Select(t => new
        {
            name = t.Name,
            lat = t.Latitude,
            lng = t.Longitude,
            route = t.RouteGeoJson,
            diff = t.Difficulty?.Label ?? "",
            url = Url.Action("Details", new { id = t.Id }),
        }));

        // paginate (page size 9), counts from filtered/pre-paged list
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
        ViewBag.UserLat = lat;
        ViewBag.UserLng = lng;
        // distances shown on cards, rounded to 0.1 km (keyed by trail id)
        ViewBag.Distances = distances?.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value, 1));

        ViewBag.FavoriteTrailIds = await GetFavoriteTrailIdsAsync();
        return View(trails);
    }

    // GET: Trails/Details/5
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
        return View(trail);
    }

    // GET: Trails/Nearby?lat=  — coords come from browser geolocation
    public async Task<IActionResult> Nearby(double? lat, double? lng)
    {
        var trails = await _context.Trails
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
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

    // GET: Trails/Create
    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateRegionsAsync();
        await PopulateTagsAsync();
        return View(new TrailFormViewModel());
    }

    // POST: Trails/Create
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
            PhotoUrl = form.PhotoUrl,
            RouteGeoJson = form.RouteGeoJson,
            RegionId = form.RegionId,
            DifficultyId = await _difficulty.ResolveDifficultyIdAsync(form.DistanceKm, form.ElevationGainM),
            AuthorId = _userManager.GetUserId(User),
            CreatedAt = DateTime.UtcNow,
            TrailTags = form.SelectedTagIds.Select(tid => new TrailTag { TagId = tid }).ToList(),
        };

        _context.Add(trail);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Trails/Edit/5
    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var trail = await _context.Trails
            .Include(t => t.TrailTags)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (trail == null) return NotFound();

        await PopulateRegionsAsync(trail.RegionId);
        await PopulateTagsAsync();

        var form = ToForm(trail);
        form.SelectedTagIds = trail.TrailTags.Select(tt => tt.TagId).ToList();
        return View(form);
    }

    // POST: Trails/Edit/5
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
            return View(form);
        }

        var trail = await _context.Trails
            .Include(t => t.TrailTags)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (trail == null) return NotFound();

        trail.Name = form.Name;
        trail.ShortDescription = form.ShortDescription;
        trail.Description = form.Description;
        trail.DistanceKm = form.DistanceKm;
        trail.ElevationGainM = form.ElevationGainM;
        trail.Latitude = form.Latitude;
        trail.Longitude = form.Longitude;
        trail.PhotoUrl = form.PhotoUrl;
        trail.RouteGeoJson = form.RouteGeoJson;
        trail.RegionId = form.RegionId;
        // recompute - distance/gain may have changed
        trail.DifficultyId = await _difficulty.ResolveDifficultyIdAsync(form.DistanceKm, form.ElevationGainM);

        // replace tag links with the new selection
        trail.TrailTags.Clear();
        foreach (var tid in form.SelectedTagIds)
            trail.TrailTags.Add(new TrailTag { TagId = tid });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TrailExists(form.Id)) return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Trails/Delete/5
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

    // POST: Trails/Delete/5
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

    // The route comes from the click-to-draw map editor as a GeoJSON LineString.
    // Blank it if empty; reject anything that isn't a valid LineString with 2+ points.
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

    // trail ids the current user has favorited (empty set when signed out) — drives the heart state
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
        PhotoUrl = t.PhotoUrl,
        RouteGeoJson = t.RouteGeoJson,
        RegionId = t.RegionId,
    };
}
