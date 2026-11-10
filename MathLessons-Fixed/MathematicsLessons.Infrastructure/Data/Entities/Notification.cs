using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey(nameof(User))]
    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Title { get; set; } = "";

    [Required, MaxLength(500)]
    public string Body { get; set; } = "";

    [MaxLength(200)]
    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
