using ascent_app.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TrailsController : Controller
{
    private readonly AscentDbContext _context;

    public TrailsController(AscentDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var trails = await _context.Trails
            .Include(t => t.Region)
            .Include(t => t.Difficulty)
            .Include(t => t.Author)
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync();

        return View(trails);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var trail = await _context.Trails.FindAsync(id);
        if (trail == null)
        {
            TempData["Err"] = "Trail not found.";
            return RedirectToAction(nameof(Index));
        }

        _context.Trails.Remove(trail);
        await _context.SaveChangesAsync();
        TempData["Ok"] = "Trail deleted.";
        return RedirectToAction(nameof(Index));
    }
}
