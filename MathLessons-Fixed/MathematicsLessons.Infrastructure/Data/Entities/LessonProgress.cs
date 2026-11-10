using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>Join User ↔ Lesson (many-to-many) with mastery percent.</summary>
public class LessonProgress
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey(nameof(User))]
    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;

    [Required, ForeignKey(nameof(Lesson))]
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    [Range(0, 100)]
    public int WatchedPercent { get; set; }

    public bool VideoWatched { get; set; }
    public bool ExercisesPassed { get; set; }
    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
