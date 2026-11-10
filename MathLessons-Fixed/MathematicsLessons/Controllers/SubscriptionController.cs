using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

/// <summary>Guest → subscribe Monthly or Annual (simulated pay).</summary>
[Authorize]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subs;
    private readonly UserManager<User> _users;
    private readonly ApplicationDbContext _ctx;

    public SubscriptionController(ISubscriptionService subs, UserManager<User> users, ApplicationDbContext ctx)
    {
        _subs = subs; _users = users; _ctx = ctx;
    }

    [HttpGet]
    public async Task<IActionResult> Plans()
    {
        var plans = await _subs.GetPlansAsync();
        var me = await _users.GetUserAsync(User);
        if (me != null) me = await _ctx.Users.Include(u => u.Sub).FirstAsync(u => u.Id == me.Id);
        ViewBag.CurrentPlan = me?.Sub?.Type;
        ViewBag.BillingCycle = me?.BillingCycle;
        ViewBag.IsLoggedIn = me != null;
        ViewBag.IsSubscriber = me?.SubId != null;
        ViewBag.IsSubscribed = _subs.IsSubscribed(me);
        return View(plans);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Choose(int planId, string billingCycle)
    {
        if (await _subs.GetPlanAsync(planId) == null) return RedirectToAction(nameof(Plans));
        HttpContext.Session.SetInt32("PendingPlanId", planId);
        HttpContext.Session.SetString("PendingCycle",
            string.Equals(billingCycle, "Annual", StringComparison.OrdinalIgnoreCase) ? "Annual" : "Monthly");
        return RedirectToAction(nameof(Checkout));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var planId = HttpContext.Session.GetInt32("PendingPlanId");
        var cycle = HttpContext.Session.GetString("PendingCycle") ?? "Monthly";
        if (planId == null) return RedirectToAction(nameof(Plans));
        var plan = await _subs.GetPlanAsync(planId.Value);
        if (plan == null) return RedirectToAction(nameof(Plans));
        ViewBag.Cycle = cycle;
        ViewBag.Amount = cycle == "Annual"
            ? (plan.PricePerYear > 0 ? plan.PricePerYear : plan.PricePerMonth * 10)
            : plan.PricePerMonth;
        return View(plan);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();
        var planId = HttpContext.Session.GetInt32("PendingPlanId");
        var cycle = HttpContext.Session.GetString("PendingCycle") ?? "Monthly";
        if (planId == null) return RedirectToAction(nameof(Plans));

        var (ok, msg, _) = await _subs.ActivateAsync(me, planId.Value, cycle);
        HttpContext.Session.Remove("PendingPlanId");
        HttpContext.Session.Remove("PendingCycle");
        TempData[ok ? "Success" : "Error"] = msg;
        if (ok) TempData["PlayNotify"] = "1";
        return ok ? RedirectToAction("Index", "Classroom") : RedirectToAction(nameof(Plans));
    }
}
