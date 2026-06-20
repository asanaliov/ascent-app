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

    // GET: Trails?q=&regionId=&difficultyId=&tagId=
    public async Task<IActionResult> Index(string? q, int? regionId, int? difficultyId, int? tagId)
    {
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

        var regions = await _context.Regions.OrderBy(r => r.Name).AsNoTracking().ToListAsync();
        var difficulties = await _context.Difficulties.AsNoTracking().ToListAsync();
        ViewBag.Regions = new SelectList(regions, "Id", "Name", regionId);
        ViewBag.Difficulties = new SelectList(difficulties, "Id", "Label", difficultyId);
        ViewBag.Tags = await _context.Tags.OrderBy(t => t.Name).AsNoTracking().ToListAsync();
        ViewBag.Filter = new TrailFilterViewModel
        {
            Q = q,
            RegionId = regionId,
            DifficultyId = difficultyId,
            TagId = tagId,
        };

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
        RegionId = t.RegionId,
    };
}
