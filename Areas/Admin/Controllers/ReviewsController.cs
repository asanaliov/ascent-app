using ascent_app.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly AscentDbContext _context;

    public ReviewsController(AscentDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var reviews = await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Trail)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return View(reviews);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            TempData["Err"] = "Review not found.";
            return RedirectToAction(nameof(Index));
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        TempData["Ok"] = "Review deleted.";
        return RedirectToAction(nameof(Index));
    }
}
