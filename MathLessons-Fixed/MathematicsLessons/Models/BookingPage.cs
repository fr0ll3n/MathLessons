using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Models;

public class BookingPage
{
    public bool IsSubscribed { get; set; }
    public string? PlanName { get; set; }
    public List<AppointmentPlan> Plans { get; set; } = new();
    public List<Appointment> Mine { get; set; } = new();
    public List<Schedule> PublicSessions { get; set; } = new();
    public int SessionsLeftThisMonth { get; set; }
    /// Next upcoming private session for the live panel.
    public Appointment? Upcoming { get; set; }
}
