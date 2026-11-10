using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MathematicsLessons.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;

    public HomeController(ApplicationDbContext ctx, UserManager<User> users)
    {
        _ctx = ctx;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = await _users.GetUserAsync(User);
        if (me != null)
            me = await _ctx.Users.Include(u => u.Sub).FirstAsync(u => u.Id == me.Id);

        var levels = await _ctx.Levels
            .OrderBy(l => l.SortOrder)
            .Select(l => new CourseStatVm
            {
                Id = l.Id,
                Name = l.Name,
                GradeRange = l.GradeRange,
                Description = l.Description,
                LessonCount = l.Lessons.Count,
                TopicCount = l.Topics.Count,
                SortOrder = l.SortOrder
            })
            .ToListAsync();

        var totalLessons = levels.Sum(l => l.LessonCount);
        var totalTopics = levels.Sum(l => l.TopicCount);

        ViewBag.IsLoggedIn = me != null;
        ViewBag.IsSubscribed = me?.SubId != null;
        ViewBag.PlanName = me?.Sub?.Type;
        ViewBag.Levels = levels;
        ViewBag.TotalLessons = totalLessons;
        ViewBag.TotalTopics = totalTopics;
        ViewBag.TotalCourses = levels.Count;
        ViewBag.Plans = await _ctx.Subs.OrderBy(s => s.PricePerMonth).ToListAsync();
        return View();
    }

    [HttpGet]
    public IActionResult About() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

public class CourseStatVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? GradeRange { get; set; }
    public string? Description { get; set; }
    public int LessonCount { get; set; }
    public int TopicCount { get; set; }
    public int SortOrder { get; set; }
}
