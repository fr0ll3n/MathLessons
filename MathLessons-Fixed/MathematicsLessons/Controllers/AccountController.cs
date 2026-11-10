using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Models;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<User> _users;
    private readonly ApplicationDbContext _ctx;
    private readonly ISubscriberRankService _ranks;
    private readonly ICertificateService _certs;

    public AccountController(UserManager<User> users, ApplicationDbContext ctx, ISubscriberRankService ranks, ICertificateService certs)
    {
        _users = users; _ctx = ctx; _ranks = ranks; _certs = certs;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();
        me = await _ctx.Users.Include(u => u.Sub).FirstAsync(u => u.Id == me.Id);
        var completed = await _ctx.LessonProgress.CountAsync(p => p.UserId == me.Id && p.IsCompleted);
        return View(new ProfilePage
        {
            User = me,
            CompletedLessons = completed,
            Certificates = await _certs.GetForUserAsync(me.Id),
            Rank = _ranks.GetRank(me)
        });
    }
}
