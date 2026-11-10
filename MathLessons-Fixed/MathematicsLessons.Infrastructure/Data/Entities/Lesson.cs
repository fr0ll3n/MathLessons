using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>Single video/text lesson. Sequential unlock within level.</summary>
public class Lesson
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    public string Content { get; set; } = "";

    [MaxLength(400)]
    public string? Video { get; set; }

    [Range(1, 1000)]
    public int SortOrder { get; set; }

    [Range(0, 600)]
    public int DurationMinutes { get; set; } = 15;

    [Range(0, 1000)]
    public int PointsReward { get; set; } = 25;

    [Required, ForeignKey(nameof(Level))]
    public int LevelId { get; set; }
    public Level Level { get; set; } = null!;

    [Required, ForeignKey(nameof(Topic))]
    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;

    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    public ICollection<LessonProgress> ProgressEntries { get; set; } = new List<LessonProgress>();
}
