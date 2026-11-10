using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Services;

public class ProgressService : IProgressService
{
    private readonly ApplicationDbContext _ctx;

    public ProgressService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<HashSet<int>> GetCompletedLessonIdsAsync(string userId) =>
        (await _ctx.LessonProgress
            .Where(p => p.UserId == userId && p.IsCompleted)
            .Select(p => p.LessonId)
            .ToListAsync()).ToHashSet();

    public async Task<LessonProgress> RecordWatchAsync(string userId, int lessonId, int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        var row = await _ctx.LessonProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);
        if (row == null)
        {
            row = new LessonProgress { UserId = userId, LessonId = lessonId };
            _ctx.LessonProgress.Add(row);
        }

        row.WatchedPercent = Math.Max(row.WatchedPercent, percent);
        row.UpdatedAt = DateTime.UtcNow;
        if (percent >= 100 && !row.IsCompleted)
        {
            row.IsCompleted = true;
            row.CompletedAt = DateTime.UtcNow;
        }
        await _ctx.SaveChangesAsync();
        return row;
    }

    public async Task<bool> IsLevelCompleteAsync(string userId, int levelId)
    {
        var lessonIds = await _ctx.Lessons.Where(l => l.LevelId == levelId).Select(l => l.Id).ToListAsync();
        if (lessonIds.Count == 0) return false;
        var done = await _ctx.LessonProgress.CountAsync(p =>
            p.UserId == userId && p.IsCompleted && lessonIds.Contains(p.LessonId));
        return done == lessonIds.Count;
    }
}
