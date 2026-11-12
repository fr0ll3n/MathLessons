using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _ctx;
    private readonly INotificationService _notify;

    public BookingService(ApplicationDbContext ctx, INotificationService notify)
    {
        _ctx = ctx;
        _notify = notify;
    }

    public Task<List<AppointmentPlan>> GetDurationPlansAsync() =>
        _ctx.AppointmentPlans.OrderBy(p => p.DurationMinutes).ToListAsync();

    public async Task<int> SessionsUsedThisMonthAsync(string userId)
    {
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        return await _ctx.Appointments.CountAsync(a =>
            a.UserId == userId && a.Date >= monthStart && a.Status != "Cancelled");
    }

    public async Task<(bool Ok, string Message, Appointment? Appointment)> CompletePaidBookingAsync(
        User user, DateTime start, int planId, string? notes)
    {
        if (user.Sub == null || !user.Sub.IncludesPrivateSessions)
            return (false, "Private sessions require Standard or Premium.", null);

        var plan = await _ctx.AppointmentPlans.FindAsync(planId);
        if (plan == null) return (false, "Invalid duration plan.", null);

        if (start < DateTime.Now.AddHours(2))
            return (false, "Pick a time at least 2 hours from now.", null);

        var used = await SessionsUsedThisMonthAsync(user.Id);
        if (used >= user.Sub.MaxSessionsPerMonth)
            return (false, $"Monthly limit reached ({user.Sub.MaxSessionsPerMonth}).", null);

        var dayCount = await CountSessionsOnDayAsync(start.Date);
        if (dayCount >= MaxSessionsPerDay)
            return (false, "That day is full (4 sessions already booked).", null);

        if (await _ctx.Appointments.AnyAsync(a => a.Date == start && a.Status == "Booked"))
            return (false, "That slot is taken.", null);

        await using var tx = await _ctx.Database.BeginTransactionAsync();

        var receipt = new Receipt
        {
            SubTotal = plan.Price,
            Kind = "Session",
            CreatedAt = DateTime.UtcNow
        };
        _ctx.Receipts.Add(receipt);
        await _ctx.SaveChangesAsync();

        var meetCode = $"{Guid.NewGuid().ToString("N")[..3]}-{Guid.NewGuid().ToString("N")[..4]}-{Guid.NewGuid().ToString("N")[..3]}";
        var appt = new Appointment
        {
            UserId = user.Id,
            Date = start,
            DurationMinutes = plan.DurationMinutes,
            AppointmentPlanId = plan.Id,
            ReceiptId = receipt.Id,
            Status = "Booked",
            Notes = notes,
            MeetingLink = $"https://meet.google.com/{meetCode}"
        };
        _ctx.Appointments.Add(appt);

        try
        {
            await _ctx.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // The unique index on a booked slot rejected a concurrent double-booking;
            // rolling back also discards the receipt created above.
            await tx.RollbackAsync();
            return (false, "That slot was just taken. Please pick another time.", null);
        }

        await _notify.NotifyAsync(user.Id, "Session booked",
            $"{plan.Name} on {start:dd MMM yyyy HH:mm}. Meet opens at session time (max 5 min late).",
            $"/Booking/Session/{appt.Id}");

        return (true, $"Paid {plan.Price:0.00} BGN. Session booked.", appt);
    }

    public async Task MarkMissedIfLateAsync(Appointment appt, DateTime now)
    {
        if (appt.Status == "Booked" && now > appt.Date.AddMinutes(5))
        {
            appt.Status = "Missed";
            await _ctx.SaveChangesAsync();
        }
    }


    public const int MaxSessionsPerDay = 4;

    public async Task<int> CountSessionsOnDayAsync(DateTime day)
    {
        var start = day.Date;
        var end = start.AddDays(1);
        return await _ctx.Appointments.CountAsync(a =>
            a.Date >= start && a.Date < end && a.Status != "Cancelled" && a.Status != "Missed");
    }

    public async Task<List<string>> GetAvailableHoursAsync(DateTime day, int durationMinutes)
    {
        // Working hours in Bulgaria local time (EET/EEST) — 09:00–18:00
        var hours = new List<string>();
        var dayCount = await CountSessionsOnDayAsync(day);
        if (dayCount >= MaxSessionsPerDay)
            return hours;

        var taken = await _ctx.Appointments
            .Where(a => a.Date >= day.Date && a.Date < day.Date.AddDays(1) && a.Status == "Booked")
            .Select(a => a.Date)
            .ToListAsync();

        for (var h = 9; h <= 17; h++)
        {
            var slot = day.Date.AddHours(h);
            // skip past slots if today
            if (slot < DateTime.Now.AddHours(2)) continue;
            if (taken.Any(t => t == slot)) continue;
            hours.Add($"{h:00}:00");
        }
        return hours;
    }

    public async Task<(bool Ok, string Message)> TryJoinAsync(Appointment appt, DateTime now)
    {
        if (now < appt.Date.AddMinutes(-2))
            return (false, "Too early. Meet link opens 2 minutes before start.");
        if (now > appt.Date.AddMinutes(5))
        {
            appt.Status = "Missed";
            await _ctx.SaveChangesAsync();
            return (false, "More than 5 minutes late. Session marked missed.");
        }
        appt.Status = "InProgress";
        await _ctx.SaveChangesAsync();
        return (true, "OK");
    }
}
