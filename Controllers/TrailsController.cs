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
    private readonly UserManager<ApplicationUser> _userManager;

    public TrailsController(
        AscentDbContext context,
        IDifficultyService difficulty,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _difficulty = difficulty;
        _userManager = userManager;
    }

    // GET: Trails
    public async Task<IActionResult> Index()
    {
        var trails = await _context.Trails
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
            .Include(t => t.Reviews)
            .AsNoTracking()
            .ToListAsync();
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
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (trail == null) return NotFound();

        return View(trail);
    }

    // GET: Trails/Create
    [Authorize(Roles = "Guide,Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateRegionsAsync();
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

        var trail = await _context.Trails.FindAsync(id);
        if (trail == null) return NotFound();

        await PopulateRegionsAsync(trail.RegionId);
        return View(ToForm(trail));
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
            return View(form);
        }

        var trail = await _context.Trails.FindAsync(id);
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

    private async Task PopulateRegionsAsync(int? selected = null)
    {
        var regions = await _context.Regions.OrderBy(r => r.Name).AsNoTracking().ToListAsync();
        ViewBag.RegionId = new SelectList(regions, "Id", "Name", selected);
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
