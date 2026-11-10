using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathematicsLessons.Infrastructure.Data.Entities;

/// <summary>Private tutoring length tiers: 1h / 1.5h / 2h.</summary>
public class AppointmentPlan
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Name { get; set; } = "";

    [Range(15, 480)]
    public int DurationMinutes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 10000)]
    public decimal Price { get; set; }

    [MaxLength(200)]
    public string Description { get; set; } = "";

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
