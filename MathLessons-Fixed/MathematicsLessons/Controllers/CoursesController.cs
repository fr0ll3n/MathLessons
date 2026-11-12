using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

/// Alternate course browse API (logic): dashboard by subscription, lessons by level, lesson details.
/// Complements ClassroomController (which has unlock rules).
public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _users;

    public CoursesController(ApplicationDbContext context, UserManager<User> users)
    {
        _context = context;
        _users = users;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();

        me = await _context.Users.Include(u => u.Sub).FirstAsync(u => u.Id == me.Id);
        if (me.SubId == null || me.Sub == null)
            return RedirectToAction("Plans", "Subscription");

        var allLevels = await _context.Levels.OrderBy(l => l.SortOrder).ToListAsync();
        ViewBag.UserTier = me.Sub.Type;
        return View(allLevels);
    }

    [HttpGet]
    public async Task<IActionResult> LessonsByLevel(int id)
    {
        var levelWithLessons = await _context.Levels
            .Include(l => l.Lessons.OrderBy(x => x.SortOrder))
            .ThenInclude(l => l.Topic)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (levelWithLessons == null) return NotFound();
        return View(levelWithLessons);
    }

    [HttpGet]
    public async Task<IActionResult> LessonDetails(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Topic)
            .Include(l => l.Exercises)
            .Include(l => l.Level)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null) return NotFound();
        // Prefer full classroom lesson page with unlock logic
        return RedirectToAction("Lesson", "Classroom", new { id });
    }
}
