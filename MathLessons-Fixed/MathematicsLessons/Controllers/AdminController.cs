using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public AdminController(ApplicationDbContext ctx, UserManager<User> users, RoleManager<IdentityRole> roles)
    {
        _ctx = ctx;
        _users = users;
        _roles = roles;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var vm = new AdminStatsVm
        {
            TotalUsers = await _ctx.Users.CountAsync(),
            TotalSubscribers = await _ctx.Users.CountAsync(u => u.SubId != null),
            TotalTeachers = (await _users.GetUsersInRoleAsync("Teacher")).Count,
            TotalLessons = await _ctx.Lessons.CountAsync(),
            LessonsWithoutVideo = await _ctx.Lessons.CountAsync(l => string.IsNullOrEmpty(l.Video)),
            TotalCourses = await _ctx.Levels.CountAsync(),
            BookingsThisMonth = await _ctx.Appointments.CountAsync(a =>
                a.Date >= monthStart && a.Status != "Cancelled"),
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? q)
    {
        var query = _ctx.Users.Include(u => u.Sub).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(q)) ||
                u.FirstName.Contains(q) ||
                u.LastName.Contains(q));
        }
        var list = await query.OrderBy(u => u.Email).Take(300).ToListAsync();

        // Batch role lookup (one query) instead of GetRolesAsync per user (N+1).
        var ids = list.Select(u => u.Id).ToList();
        var rolePairs = await (from ur in _ctx.UserRoles
                               join r in _ctx.Roles on ur.RoleId equals r.Id
                               where ids.Contains(ur.UserId)
                               select new { ur.UserId, r.Name })
                              .ToListAsync();
        var rolesByUser = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToList());

        var rows = list.Select(u =>
        {
            var roles = rolesByUser.TryGetValue(u.Id, out var r) ? r : new List<string>();
            return new AdminUserRow
            {
                User = u,
                IsTeacher = roles.Contains("Teacher"),
                IsAdmin = roles.Contains("Admin"),
                Roles = string.Join(", ", roles)
            };
        }).ToList();
        ViewBag.Search = q;
        return View(rows);
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeTeacherByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Въведи email.";
            return RedirectToAction(nameof(Users));
        }
        if (!await _roles.RoleExistsAsync("Teacher"))
            await _roles.CreateAsync(new IdentityRole("Teacher"));
        var u = await _users.FindByEmailAsync(email.Trim());
        if (u == null)
        {
            TempData["Error"] = "Няма потребител с този email.";
            return RedirectToAction(nameof(Users));
        }
        if (!await _users.IsInRoleAsync(u, "Teacher"))
            await _users.AddToRoleAsync(u, "Teacher");
        TempData["Success"] = $"{u.Email} вече е Teacher.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeTeacher(string id)
    {
        if (!await _roles.RoleExistsAsync("Teacher"))
            await _roles.CreateAsync(new IdentityRole("Teacher"));
        var u = await _users.FindByIdAsync(id);
        if (u != null && !await _users.IsInRoleAsync(u, "Teacher"))
            await _users.AddToRoleAsync(u, "Teacher");
        TempData["Success"] = "Teacher role added.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeacher(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u != null && await _users.IsInRoleAsync(u, "Teacher"))
            await _users.RemoveFromRoleAsync(u, "Teacher");
        TempData["Success"] = "Teacher role removed.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeAdmin(string id)
    {
        if (!await _roles.RoleExistsAsync("Admin"))
            await _roles.CreateAsync(new IdentityRole("Admin"));
        var u = await _users.FindByIdAsync(id);
        if (u != null && !await _users.IsInRoleAsync(u, "Admin"))
            await _users.AddToRoleAsync(u, "Admin");
        TempData["Success"] = "Admin role added.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Plans() =>
        View(await _ctx.Subs.OrderBy(s => s.PricePerMonth).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> EditPlan(int id)
    {
        var p = await _ctx.Subs.FindAsync(id);
        return p == null ? NotFound() : View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPlan(int id, Sub model)
    {
        if (id != model.Id) return BadRequest();
        _ctx.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Plan saved.";
        return RedirectToAction(nameof(Plans));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var plan = await _ctx.Subs.FindAsync(id);
        if (plan != null)
        {
            _ctx.Subs.Remove(plan);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Plan deleted.";
        }
        return RedirectToAction(nameof(Plans));
    }

    [HttpGet]
    public async Task<IActionResult> SessionPlans() =>
        View(await _ctx.AppointmentPlans.OrderBy(p => p.DurationMinutes).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSessionPlan(AppointmentPlan model)
    {
        if (model.Id == 0) _ctx.AppointmentPlans.Add(model);
        else _ctx.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Session plan saved.";
        return RedirectToAction(nameof(SessionPlans));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSessionPlan(int id)
    {
        var p = await _ctx.AppointmentPlans.FindAsync(id);
        if (p != null) { _ctx.Remove(p); await _ctx.SaveChangesAsync(); }
        return RedirectToAction(nameof(SessionPlans));
    }

    [HttpGet]
    public async Task<IActionResult> PublicSessions() =>
        View(await _ctx.Schedules.OrderByDescending(s => s.SelectedDateTime).Take(50).ToListAsync());

    [HttpGet]
    public IActionResult CreatePublicSession() => View(new Schedule
    {
        SelectedDateTime = DateTime.Now.AddDays(1).Date.AddHours(18),
        DurationMinutes = 60,
        IsPublic = true,
        DiscordChannel = "#public-sessions",
        Title = "Q&A session"
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePublicSession(Schedule model)
    {
        model.IsPublic = true;
        _ctx.Schedules.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Public session created.";
        return RedirectToAction(nameof(PublicSessions));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePublicSession(int id)
    {
        var s = await _ctx.Schedules.FindAsync(id);
        if (s != null) { _ctx.Remove(s); await _ctx.SaveChangesAsync(); }
        return RedirectToAction(nameof(PublicSessions));
    }

    [HttpGet]
    public async Task<IActionResult> Bookings() =>
        View(await _ctx.Appointments.Include(a => a.User).Include(a => a.AppointmentPlan)
            .OrderByDescending(a => a.Date).Take(100).ToListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    private async Task SeedGrade(int levelId, int grade, string video, params (string subject, string[] lessons)[] subjects)
    {
        int order = grade * 10;
        foreach (var (subject, lessons) in subjects)
            await SeedTopic(levelId, $"{grade} клас — {subject}", order++, video, lessons);
    }

    private async Task SeedTopic(int levelId, string name, int order, string video, params string[] titles)
    {
        var topic = new Topic
        {
            LevelId = levelId,
            Name = name,
            Description = name,
            SortOrder = order
        };
        _ctx.Topics.Add(topic);
        await _ctx.SaveChangesAsync();

        int lo = order * 10;
        foreach (var title in titles)
        {
            var lesson = new Lesson
            {
                LevelId = levelId,
                TopicId = topic.Id,
                Title = title,
                Content = BuildLessonContent(title),
                Video = video,
                SortOrder = lo++,
                DurationMinutes = 15,
                PointsReward = 25
            };
            _ctx.Lessons.Add(lesson);
            await _ctx.SaveChangesAsync();

            // materials: 2 practice exercises per lesson
            _ctx.Exercises.Add(new Exercise
            {
                LessonId = lesson.Id,
                Question = $"Кратко: какво научи от „{title}“? (напиши ключова дума от заглавието)",
                Solution = title.Split(' ').FirstOrDefault() ?? "ok",
                Hint = "Погледни заглавието на урока.",
                PointsReward = 10
            });
            _ctx.Exercises.Add(new Exercise
            {
                LessonId = lesson.Id,
                Question = "Готов ли си за следващия урок? (напиши yes или да)",
                Solution = "yes",
                Hint = "Отговори с yes или да",
                PointsReward = 5
            });
            await _ctx.SaveChangesAsync();
        }
    }

    private static string BuildLessonContent(string title)
    {
        return $"Урок: {title}.\n\n" +
               "1) Гледай видеото докрай.\n" +
               "2) Прочети кратките бележки.\n" +
               "3) Реши упражненията под видеото — при грешка гледай урока отново.\n" +
               "4) След успех се отключва следващият урок в секцията.";
    }
}


public class AdminStatsVm
{
    public int TotalUsers { get; set; }
    public int TotalSubscribers { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalLessons { get; set; }
    public int LessonsWithoutVideo { get; set; }
    public int TotalCourses { get; set; }
    public int BookingsThisMonth { get; set; }
}

public class AdminUserRow
{
    public User User { get; set; } = null!;
    public bool IsTeacher { get; set; }
    public bool IsAdmin { get; set; }
    public string Roles { get; set; } = "";
}
