using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

[Authorize(Roles = "Teacher,Admin")]
public class TeacherController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public TeacherController(ApplicationDbContext ctx) => _ctx = ctx;

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var missing = await _ctx.Lessons
            .Include(l => l.Level)
            .Where(l => l.Video == null || l.Video == "")
            .OrderBy(l => l.Level.SortOrder).ThenBy(l => l.SortOrder)
            .Take(20)
            .ToListAsync();

        var upcoming = await _ctx.Appointments
            .Include(a => a.User)
            .Include(a => a.AppointmentPlan)
            .Where(a => a.Date > DateTime.Now && a.Status == "Booked")
            .OrderBy(a => a.Date)
            .Take(15)
            .ToListAsync();

        ViewBag.ManageCoursesUrl = Url.Action("Index", "CourseAdmin");
        return View(new TeacherDashboard
        {
            TotalStudents = await _ctx.Users.CountAsync(),
            LessonsWithoutVideo = await _ctx.Lessons.CountAsync(l => l.Video == null || l.Video == ""),
            UpcomingBookings = upcoming.Count,
            RecentBookings = upcoming,
            MissingVideos = missing
        });
    }
}
