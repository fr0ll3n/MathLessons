using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>RSVP join User ↔ Schedule (many-to-many).</summary>
public class SessionAttendance
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey(nameof(User))]
    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;

    [Required, ForeignKey(nameof(Schedule))]
    public int ScheduleId { get; set; }
    public Schedule Schedule { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
