using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;

namespace MathematicsLessons.Services;

public class LessonAccessService : ILessonAccessService
{
    public bool IsLevelUnlocked(Level level, IReadOnlyList<Level> allLevels, IReadOnlyList<Lesson> allLessons, IReadOnlySet<int> completedLessonIds)
    {
        foreach (var earlier in allLevels.Where(l => l.SortOrder < level.SortOrder))
        {
            var ids = allLessons.Where(l => l.LevelId == earlier.Id).Select(l => l.Id).ToList();
            if (ids.Count == 0) continue;
            if (ids.Any(id => !completedLessonIds.Contains(id)))
                return false;
        }
        return true;
    }

    public bool IsLessonUnlocked(Lesson lesson, IReadOnlyList<Level> allLevels, IReadOnlyList<Lesson> allLessons, IReadOnlySet<int> completedLessonIds)
    {
        var level = allLevels.FirstOrDefault(l => l.Id == lesson.LevelId);
        if (level == null) return false;
        if (!IsLevelUnlocked(level, allLevels, allLessons, completedLessonIds))
            return false;

        var previous = allLessons
            .Where(l => l.LevelId == lesson.LevelId && l.SortOrder < lesson.SortOrder)
            .OrderByDescending(l => l.SortOrder)
            .FirstOrDefault();
        return previous == null || completedLessonIds.Contains(previous.Id);
    }
}
