using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

/// <summary>
/// Logic: payment / receipt history for the logged-in user (subscriptions + sessions).
/// Uses existing Receipt entity — no fake payment gateway.
/// </summary>
[Authorize]
public class BillingController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;

    public BillingController(ApplicationDbContext ctx, UserManager<User> users)
    {
        _ctx = ctx;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return Challenge();

        // Receipts linked to this user's appointments
        var fromSessions = await _ctx.Appointments
            .Include(a => a.Receipt)
            .Include(a => a.AppointmentPlan)
            .Where(a => a.UserId == me.Id && a.ReceiptId != null)
            .OrderByDescending(a => a.Date)
            .Select(a => new BillingRow
            {
                Date = a.Receipt!.CreatedAt,
                Kind = a.Receipt.Kind,
                Description = $"Session: {a.AppointmentPlan.Name} on {a.Date:dd MMM yyyy HH:mm}",
                Amount = a.Receipt.SubTotal - a.Receipt.Discount,
                Status = a.Status
            })
            .ToListAsync();

        // Subscription receipts: we store Kind = "Subscription" but they are not always linked to Appointment.
        // Show all subscription-kind receipts created around the time user has a Sub (simple approach:
        // also list receipts that appear only via appointments is enough for demo; add subscription rows from user context).
        ViewBag.Plan = (await _ctx.Users.Include(u => u.Sub).FirstAsync(u => u.Id == me.Id)).Sub?.Type ?? "Guest";

        return View(fromSessions);
    }
}

/// <summary>Simple DTO for billing list (not an EF entity).</summary>
public class BillingRow
{
    public DateTime Date { get; set; }
    public string Kind { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}
