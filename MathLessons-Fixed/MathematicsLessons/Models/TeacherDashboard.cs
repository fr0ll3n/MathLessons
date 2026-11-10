using MathematicsLessons.Infrastructure.Data.Entities;

namespace MathematicsLessons.Models;

public class TeacherDashboard
{
    public int TotalStudents { get; set; }
    public int LessonsWithoutVideo { get; set; }
    public int UpcomingBookings { get; set; }
    public List<Appointment> RecentBookings { get; set; } = new();
    public List<Lesson> MissingVideos { get; set; } = new();
}
