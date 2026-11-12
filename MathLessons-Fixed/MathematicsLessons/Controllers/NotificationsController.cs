using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

/// <summary>
/// Logic: list user notifications, mark one or all as read.
/// </summary>
[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;

    public NotificationsController(ApplicationDbContext ctx, UserManager<User> users)
    {
        _ctx = ctx;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();

        var list = await _ctx.Notifications
            .Where(n => n.UserId == me.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();

        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();

        var n = await _ctx.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == me.Id);
        if (n != null)
        {
            n.IsRead = true;
            await _ctx.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();

        var unread = await _ctx.Notifications
            .Where(n => n.UserId == me.Id && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
