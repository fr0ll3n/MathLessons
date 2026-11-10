using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Services.Interfaces;

public interface IPointsService
{
    Task AwardAsync(User user, int points, string reason);
    void UpdateStreak(User user);
}
