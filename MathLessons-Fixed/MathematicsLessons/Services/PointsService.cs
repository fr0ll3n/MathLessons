using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;

namespace MathematicsLessons.Services;

public class PointsService : IPointsService
{
    private readonly ApplicationDbContext _ctx;

    public PointsService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task AwardAsync(User user, int points, string reason)
    {
        user.Points += points;
        UpdateStreak(user);
        await _ctx.SaveChangesAsync();
    }

    public void UpdateStreak(User user)
    {
        var today = DateTime.UtcNow.Date;
        if (user.LastActivityDate == null)
            user.StreakDays = 1;
        else if (user.LastActivityDate.Value.Date == today) { }
        else if (user.LastActivityDate.Value.Date == today.AddDays(-1))
            user.StreakDays += 1;
        else
            user.StreakDays = 1;
        user.LastActivityDate = today;
    }
}
