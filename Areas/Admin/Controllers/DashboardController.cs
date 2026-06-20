using ascent_app.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ascent_app.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly AscentDbContext _context;

    public DashboardController(AscentDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        ViewBag.Trails = await _context.Trails.CountAsync();
        ViewBag.Regions = await _context.Regions.CountAsync();
        ViewBag.Tags = await _context.Tags.CountAsync();
        ViewBag.Badges = await _context.Badges.CountAsync();
        ViewBag.Users = await _context.Users.CountAsync();
        ViewBag.Reviews = await _context.Reviews.CountAsync();
        ViewBag.Hikes = await _context.HikeLogs.CountAsync();
        return View();
    }
}
