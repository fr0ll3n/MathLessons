using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Services.Interfaces;

public interface ISubscriptionService
{
    Task<List<Sub>> GetPlansAsync();
    Task<Sub?> GetPlanAsync(int planId);
    /// <summary>billingCycle: "Monthly" or "Annual"</summary>
    Task<(bool Ok, string Message, Receipt? Receipt)> ActivateAsync(User user, int planId, string billingCycle);
    bool IsSubscribed(User? user);
    bool CanBookPrivateSessions(User? user);
}
