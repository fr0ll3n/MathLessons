using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

/// Public sessions = Discord live Q&A on course exercises / hard topics (not private tutoring).
[Authorize]
public class SessionsController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;

    public SessionsController(ApplicationDbContext ctx, UserManager<User> users)
    {
        _ctx = ctx; _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var upcoming = await _ctx.Schedules
            .Include(s => s.Attendances)
            .Where(s => s.IsPublic && s.SelectedDateTime > DateTime.Now.AddHours(-1))
            .OrderBy(s => s.SelectedDateTime)
            .ToListAsync();

        var me = await _users.GetUserAsync(User);
        var myIds = me == null
            ? new HashSet<int>()
            : (await _ctx.SessionAttendances.Where(a => a.UserId == me.Id).Select(a => a.ScheduleId).ToListAsync()).ToHashSet();

        ViewBag.JoinedIds = myIds;
        ViewBag.DefaultDiscord = "https://discord.gg/mathlessons"; // placeholder for diploma
        return View(upcoming);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();
        var session = await _ctx.Schedules.FindAsync(id);
        if (session == null) return NotFound();

        if (!await _ctx.SessionAttendances.AnyAsync(a => a.UserId == me.Id && a.ScheduleId == id))
        {
            _ctx.SessionAttendances.Add(new SessionAttendance { UserId = me.Id, ScheduleId = id });
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "RSVP saved. Open Discord at session time.";
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();
        var row = await _ctx.SessionAttendances.FirstOrDefaultAsync(a => a.UserId == me.Id && a.ScheduleId == id);
        if (row != null)
        {
            _ctx.SessionAttendances.Remove(row);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Left the session.";
        }
        return RedirectToAction(nameof(Index));
    }
}
