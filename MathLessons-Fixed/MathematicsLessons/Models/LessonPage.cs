using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Models;

public class LessonPage
{
    public Lesson Lesson { get; set; } = null!;
    public bool IsUnlocked { get; set; }
    public bool IsCompleted { get; set; }
    public int WatchedPercent { get; set; }
    public Lesson? NextLesson { get; set; }
    public bool CanUpload { get; set; }
    public bool VideoWatched { get; set; }
    public bool ExercisesPassed { get; set; }
    public bool CanPractice { get; set; }
    public bool? PracticeCorrect { get; set; }
    public string? PracticeMessage { get; set; }
    public List<Exercise> Exercises { get; set; } = new();
}
