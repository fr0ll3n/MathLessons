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
public class BookingController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;
    private readonly IBookingService _booking;
    private readonly ISubscriptionService _subs;

    public BookingController(ApplicationDbContext ctx, UserManager<User> users, IBookingService booking, ISubscriptionService subs)
    {
        _ctx = ctx; _users = users; _booking = booking; _subs = subs;
    }

    private async Task<User?> MeAsync()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return null;
        return await _ctx.Users.Include(u => u.Sub).FirstAsync(u => u.Id == me.Id);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        if (!_subs.CanBookPrivateSessions(me))
        {
            TempData["Error"] = "Частните уроци са за Math Enthusiast. Виж абонамента.";
            return RedirectToAction("Plans", "Subscription");
        }

        var used = await _booking.SessionsUsedThisMonthAsync(me.Id);
        var mine = await _ctx.Appointments.Include(a => a.AppointmentPlan).Include(a => a.Receipt)
            .Where(a => a.UserId == me.Id).OrderByDescending(a => a.Date).ToListAsync();
        var upcoming = mine.Where(a => a.Status is "Booked" or "InProgress" && a.Date.AddMinutes(a.DurationMinutes) >= DateTime.Now)
            .OrderBy(a => a.Date).FirstOrDefault();

        return View(new BookingPage
        {
            IsSubscribed = true,
            PlanName = me.Sub!.Type,
            Plans = await _booking.GetDurationPlansAsync(),
            Mine = mine,
            PublicSessions = await _ctx.Schedules.Where(s => s.IsPublic && s.SelectedDateTime > DateTime.Now)
                .OrderBy(s => s.SelectedDateTime).Take(5).ToListAsync(),
            SessionsLeftThisMonth = Math.Max(0, me.Sub.MaxSessionsPerMonth - used),
            Upcoming = upcoming
        });
    }

    [HttpGet]
    public async Task<IActionResult> Book(int? planId, string? date)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        if (!_subs.CanBookPrivateSessions(me))
        {
            TempData["Error"] = "Частните уроци са за Math Enthusiast.";
            return RedirectToAction("Plans", "Subscription");
        }

        var plans = await _booking.GetDurationPlansAsync();
        ViewBag.Plans = plans;
        ViewBag.SelectedPlanId = planId;
        ViewBag.MinDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        ViewBag.MaxDate = DateTime.Today.AddDays(60).ToString("yyyy-MM-dd");
        ViewBag.TimeZoneNote = "Часовете са в българско време (EET / EEST — автоматично лятно часово време).";

        AppointmentPlan? selected = null;
        if (planId != null)
            selected = plans.FirstOrDefault(p => p.Id == planId);
        ViewBag.SelectedPlan = selected;

        DateTime? selectedDate = null;
        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var d))
            selectedDate = d.Date;
        ViewBag.SelectedDate = selectedDate?.ToString("yyyy-MM-dd");

        if (selectedDate != null)
        {
            var count = await _booking.CountSessionsOnDayAsync(selectedDate.Value);
            ViewBag.DaySessionCount = count;
            ViewBag.DayFull = count >= 4;
            ViewBag.Hours = selected != null
                ? await _booking.GetAvailableHoursAsync(selectedDate.Value, selected.DurationMinutes)
                : new List<string>();
        }
        else
        {
            ViewBag.DaySessionCount = 0;
            ViewBag.DayFull = false;
            ViewBag.Hours = new List<string>();
        }

        return View();
    }

    /// JSON: how many bookings on a day (for calendar UI).
    [HttpGet]
    public async Task<IActionResult> DayStatus(string date)
    {
        if (!DateTime.TryParse(date, out var d))
            return Json(new { ok = false });
        var count = await _booking.CountSessionsOnDayAsync(d.Date);
        return Json(new { ok = true, count, full = count >= 4, max = 4 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(DateTime date, string hour, int planId, string? notes)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        if (!_subs.CanBookPrivateSessions(me))
            return RedirectToAction("Plans", "Subscription");

        if (!TimeSpan.TryParse(hour, out var time))
        {
            TempData["Error"] = "Избери дата, час и продължителност.";
            return RedirectToAction(nameof(Book), new { planId, date = date.ToString("yyyy-MM-dd") });
        }

        var dayCount = await _booking.CountSessionsOnDayAsync(date.Date);
        if (dayCount >= 4)
        {
            TempData["Error"] = "Този ден е зает (вече има 4 или повече сесии). Избери друг ден.";
            return RedirectToAction(nameof(Book), new { planId });
        }

        var start = date.Date.Add(time);
        var plan = (await _booking.GetDurationPlansAsync()).FirstOrDefault(p => p.Id == planId);
        if (plan == null)
        {
            TempData["Error"] = "Невалидна продължителност.";
            return RedirectToAction(nameof(Book));
        }

        HttpContext.Session.SetString("PendingBook", System.Text.Json.JsonSerializer.Serialize(new PendingBookDto
        {
            Date = start,
            PlanId = plan.Id,
            Notes = notes,
            Price = plan.Price,
            PlanName = plan.Name,
            DurationMinutes = plan.DurationMinutes
        }));
        return RedirectToAction(nameof(Pay));
    }

    [HttpGet]
    public IActionResult Pay()
    {
        var json = HttpContext.Session.GetString("PendingBook");
        if (string.IsNullOrEmpty(json))
        {
            TempData["Error"] = "No pending booking.";
            return RedirectToAction(nameof(Book));
        }
        ViewBag.Pending = System.Text.Json.JsonSerializer.Deserialize<PendingBookDto>(json);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment()
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var json = HttpContext.Session.GetString("PendingBook");
        if (string.IsNullOrEmpty(json)) return RedirectToAction(nameof(Book));
        var pending = System.Text.Json.JsonSerializer.Deserialize<PendingBookDto>(json)!;

        var (ok, msg, appt) = await _booking.CompletePaidBookingAsync(me, pending.Date, pending.PlanId, pending.Notes);
        HttpContext.Session.Remove("PendingBook");
        TempData[ok ? "Success" : "Error"] = msg;
        if (ok && appt != null)
        {
            TempData["PlayNotify"] = "1";
            return RedirectToAction(nameof(Session), new { id = appt.Id });
        }
        return RedirectToAction(nameof(Book));
    }

    [HttpGet]
    public async Task<IActionResult> Session(int id)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var appt = await _ctx.Appointments.Include(a => a.AppointmentPlan).Include(a => a.Receipt)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == me.Id);
        if (appt == null) return NotFound();

        var now = DateTime.Now;
        await _booking.MarkMissedIfLateAsync(appt, now);
        ViewBag.CanJoin = appt.Status is "Booked" or "InProgress"
                          && now >= appt.Date.AddMinutes(-2) && now <= appt.Date.AddMinutes(5);
        ViewBag.TooLate = appt.Status == "Missed" || now > appt.Date.AddMinutes(5);
        ViewBag.NotYet = now < appt.Date.AddMinutes(-2) && appt.Status == "Booked";
        return View(appt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var appt = await _ctx.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == me.Id);
        if (appt == null) return NotFound();
        var (ok, msg) = await _booking.TryJoinAsync(appt, DateTime.Now);
        if (!ok) { TempData["Error"] = msg; return RedirectToAction(nameof(Session), new { id }); }
        return Redirect(appt.MeetingLink ?? "/Booking");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var appt = await _ctx.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == me.Id);
        if (appt != null && appt.Date > DateTime.Now.AddHours(4) && appt.Status == "Booked")
        {
            appt.Status = "Cancelled";
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Cancelled.";
        }
        else TempData["Error"] = "Cannot cancel less than 4 hours before.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int id, int rating, string? feedback)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var appt = await _ctx.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.UserId == me.Id);
        if (appt == null) return RedirectToAction(nameof(Index));
        appt.Rating = Math.Clamp(rating, 1, 5);
        appt.Feedback = feedback;
        appt.Status = "Completed";
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

public class PendingBookDto
{
    public DateTime Date { get; set; }
    public int PlanId { get; set; }
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public string PlanName { get; set; } = "";
    public int DurationMinutes { get; set; }
}
