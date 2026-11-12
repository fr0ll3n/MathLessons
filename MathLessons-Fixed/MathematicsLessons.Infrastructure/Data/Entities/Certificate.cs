using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// Issued when a level is 100% complete (Coursera/Khan certificate idea).
public class Certificate
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Code { get; set; } = "";

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [Required, ForeignKey(nameof(User))]
    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;

    [Required, ForeignKey(nameof(Level))]
    public int LevelId { get; set; }
    public Level Level { get; set; } = null!;
}
