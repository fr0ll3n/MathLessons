using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Services.Interfaces;

/// <summary>Sequential unlock rules (Khan-style mastery path).</summary>
public interface ILessonAccessService
{
    bool IsLevelUnlocked(Level level, IReadOnlyList<Level> allLevels, IReadOnlyList<Lesson> allLessons, IReadOnlySet<int> completedLessonIds);
    bool IsLessonUnlocked(Lesson lesson, IReadOnlyList<Level> allLevels, IReadOnlyList<Lesson> allLessons, IReadOnlySet<int> completedLessonIds);
}
