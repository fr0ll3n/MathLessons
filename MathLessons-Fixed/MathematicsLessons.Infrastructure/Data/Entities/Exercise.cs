using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>Practice item after a lesson (Khan/Codecademy style check).</summary>
public class Exercise
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Question { get; set; } = "";

    [Required, MaxLength(200)]
    public string Solution { get; set; } = "";

    [MaxLength(300)]
    public string? Hint { get; set; }

    [Range(1, 1000)]
    public int PointsReward { get; set; } = 10;

    [Required, ForeignKey(nameof(Lesson))]
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
}
