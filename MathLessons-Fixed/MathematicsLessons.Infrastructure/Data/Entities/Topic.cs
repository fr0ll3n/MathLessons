using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>Unit inside a level (e.g. Algebra). Many → one Level; one → many Lessons.</summary>
public class Topic
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(300)]
    public string? Description { get; set; }

    [Range(1, 500)]
    public int SortOrder { get; set; }

    [Required, ForeignKey(nameof(Level))]
    public int LevelId { get; set; }
    public Level Level { get; set; } = null!;

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
