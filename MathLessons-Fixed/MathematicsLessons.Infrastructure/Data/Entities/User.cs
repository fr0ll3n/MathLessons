using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace MathematicsLessons.Infrastructure.Data.Entities;

public class User : IdentityUser
{
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = "";

    [Required, MaxLength(50)]
    public string LastName { get; set; } = "";

    [MaxLength(260)]
    public string? PFP { get; set; }

    [Range(0, int.MaxValue)]
    public int Points { get; set; }

    [Range(0, int.MaxValue)]
    public int StreakDays { get; set; }

    public DateTime? LastActivityDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubStartedAt { get; set; }

    /// <summary>Monthly or Annual.</summary>
    [MaxLength(20)]
    public string? BillingCycle { get; set; }

    [ForeignKey(nameof(Sub))]
    public int? SubId { get; set; }
    public Sub? Sub { get; set; }

    public ICollection<LessonProgress> LessonProgressEntries { get; set; } = new List<LessonProgress>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<SessionAttendance> SessionAttendances { get; set; } = new List<SessionAttendance>();

    [NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(FirstName)
        ? UserName ?? Email ?? "Student"
        : $"{FirstName} {LastName}".Trim();
}
