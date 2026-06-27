using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ascent_app.Data;
using ascent_app.Models;

namespace ascent_app.Controllers;

public class HomeController : Controller {
    private readonly AscentDbContext _context;

    public HomeController(AscentDbContext context) {
        _context = context;
    }

    public async Task<IActionResult> Index() {
        var trails = await _context.Trails
            .Include(t => t.Difficulty)
            .Include(t => t.Region)
            .Include(t => t.Reviews)
            .Include(t => t.Photos)
            .AsNoTracking()
            .ToListAsync();

        var popularTrails = trails
            .OrderByDescending(t => t.AverageRating)
            .ThenByDescending(t => t.Reviews.Count)
            .Take(8)
            .OrderBy(x => Guid.NewGuid())
            .ToList();

        return View(popularTrails);
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
