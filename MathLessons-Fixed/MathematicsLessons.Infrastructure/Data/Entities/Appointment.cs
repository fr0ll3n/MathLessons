using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>Paid private session. Join window: start−2min … start+5min (max late = 5 min).</summary>
public class Appointment
{
    [Key]
    public int Id { get; set; }

    [Required, ForeignKey(nameof(User))]
    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }

    [Range(15, 480)]
    public int DurationMinutes { get; set; }

    [Required, ForeignKey(nameof(AppointmentPlan))]
    public int AppointmentPlanId { get; set; }
    public AppointmentPlan AppointmentPlan { get; set; } = null!;

    [ForeignKey(nameof(Receipt))]
    public int? ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Booked";

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(400)]
    public string? MeetingLink { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }

    [MaxLength(500)]
    public string? Feedback { get; set; }
}
