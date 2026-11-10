using System.ComponentModel.DataAnnotations;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>Catalog level (Beginner / Intermediate / Advanced / University). One → many Topics & Lessons.</summary>
public class Level
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(300)]
    public string Description { get; set; } = "";

    [Required, MaxLength(50)]
    public string GradeRange { get; set; } = "";

    [Range(1, 100)]
    public int SortOrder { get; set; }

    [Required, MaxLength(30)]
    public string BadgeColor { get; set; } = "primary";

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
