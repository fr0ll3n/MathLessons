using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Services.Interfaces;

public interface IProgressService
{
    Task<HashSet<int>> GetCompletedLessonIdsAsync(string userId);
    Task<LessonProgress> RecordWatchAsync(string userId, int lessonId, int percent);
    Task<bool> IsLevelCompleteAsync(string userId, int levelId);
}
