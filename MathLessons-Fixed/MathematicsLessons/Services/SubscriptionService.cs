using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;
    private readonly INotificationService _notify;

    public SubscriptionService(ApplicationDbContext ctx, UserManager<User> users, INotificationService notify)
    {
        _ctx = ctx; _users = users; _notify = notify;
    }

    public Task<List<Sub>> GetPlansAsync() =>
        _ctx.Subs.OrderBy(s => s.PricePerMonth).ToListAsync();

    public Task<Sub?> GetPlanAsync(int planId) =>
        _ctx.Subs.FindAsync(planId).AsTask();

    public bool IsSubscribed(User? user) => user?.SubId != null;

    public bool CanBookPrivateSessions(User? user) =>
        user?.Sub != null && user.Sub.IncludesPrivateSessions;

    public async Task<(bool Ok, string Message, Receipt? Receipt)> ActivateAsync(User user, int planId, string billingCycle)
    {
        var plan = await _ctx.Subs.FindAsync(planId);
        if (plan == null) return (false, "Plan not found.", null);

        var cycle = string.Equals(billingCycle, "Annual", StringComparison.OrdinalIgnoreCase) ? "Annual" : "Monthly";
        var amount = cycle == "Annual"
            ? (plan.PricePerYear > 0 ? plan.PricePerYear : plan.PricePerMonth * 10)
            : plan.PricePerMonth;

        var receipt = new Receipt
        {
            SubTotal = amount,
            Discount = cycle == "Annual" ? Math.Max(0, plan.PricePerMonth * 12 - amount) : 0,
            Kind = $"Subscription-{cycle}",
            CreatedAt = DateTime.UtcNow
        };
        _ctx.Receipts.Add(receipt);
        await _ctx.SaveChangesAsync();

        user.SubId = plan.Id;
        user.BillingCycle = cycle;
        if (user.SubStartedAt == null)
            user.SubStartedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        await _notify.NotifyAsync(user.Id, "Subscription active",
            $"{plan.Type} ({cycle}) — {amount:0.00} BGN. Courses unlocked.",
            "/Classroom");

        return (true, $"Payment OK. {plan.Type} ({cycle}) activated for {amount:0.00} BGN.", receipt);
    }
}
