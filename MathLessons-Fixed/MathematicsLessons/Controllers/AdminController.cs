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
        var rows = new List<AdminUserRow>();
        foreach (var u in list)
        {
            var roles = await _users.GetRolesAsync(u);
            rows.Add(new AdminUserRow
            {
                User = u,
                IsTeacher = roles.Contains("Teacher"),
                IsAdmin = roles.Contains("Admin"),
                Roles = string.Join(", ", roles)
            });
        }
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
    public async Task<IActionResult> LoadDemoCourses()
    {
        if (await _ctx.Levels.AnyAsync())
        {
            TempData["Error"] = "Courses already exist. Demo catalog was not loaded.";
            return RedirectToAction(nameof(Index));
        }

        const string sampleVideo = "https://www.w3schools.com/html/mov_bbb.mp4";

        if (!await _ctx.Subs.AnyAsync())
        {
            _ctx.Subs.Add(new Sub
            {
                Type = "Free",
                PricePerMonth = 0,
                PricePerYear = 0,
                IncludesPrivateSessions = false,
                Description = "First Beginner lesson free",
                MaxSessionsPerMonth = 0
            });
            _ctx.Subs.Add(new Sub
            {
                Type = "Math Enthusiast",
                PricePerMonth = 19.99m,
                PricePerYear = 199m,
                IncludesPrivateSessions = true,
                Description = "Full access + private lessons",
                MaxSessionsPerMonth = 8
            });
        }

        if (!await _ctx.AppointmentPlans.AnyAsync())
        {
            _ctx.AppointmentPlans.AddRange(
                new AppointmentPlan { Name = "1 hour", DurationMinutes = 60, Price = 25m },
                new AppointmentPlan { Name = "1.5 hours", DurationMinutes = 90, Price = 35m },
                new AppointmentPlan { Name = "2 hours", DurationMinutes = 120, Price = 45m });
        }

        var beginner = new Level { Name = "Beginner", GradeRange = "1st – 4th grade", SortOrder = 1, Description = "Основи за 1–4 клас." };
        var intermediate = new Level { Name = "Intermediate", GradeRange = "5th – 7th grade", SortOrder = 2, Description = "5–7 клас." };
        var advanced = new Level { Name = "Advanced", GradeRange = "8th – 12th grade", SortOrder = 3, Description = "8–12 клас." };
        var college = new Level { Name = "College", GradeRange = "University Part 1–3", SortOrder = 4, Description = "University math." };
        _ctx.Levels.AddRange(beginner, intermediate, advanced, college);
        await _ctx.SaveChangesAsync();

        await SeedGrade(beginner.Id, 1, sampleVideo,
            ("Числа и броене", new[] { "Числа до 10", "Сравняване", "Числова ос" }),
            ("Събиране и изваждане", new[] { "Събиране до 10", "Изваждане до 10", "Задачи" }),
            ("Фигури", new[] { "Квадрат и кръг", "Триъгълник", "Разпознаване" }));
        await SeedGrade(beginner.Id, 2, sampleVideo,
            ("Числа до 100", new[] { "Десетици", "Събиране", "Изваждане" }),
            ("Умножение", new[] { "Увод", "Таблица 2 и 5", "Задачи" }),
            ("Фигури", new[] { "Отсечка", "Сантиметър", "Правоъгълник" }));
        await SeedGrade(beginner.Id, 3, sampleVideo,
            ("Числа до 1000", new[] { "Разряди", "Събиране", "Изваждане" }),
            ("Умножение и деление", new[] { "Таблица", "Деление", "Задачи" }),
            ("Геометрия", new[] { "Периметър", "Триъгълници", "Симетрия" }));
        await SeedGrade(beginner.Id, 4, sampleVideo,
            ("Големи числа", new[] { "Над 1000", "Закони", "Оценка" }),
            ("Дроби", new[] { "Какво е дроб", "Единични", "Сравняване" }),
            ("Лице", new[] { "Правоъгълник", "Единици", "Задачи" }));

        await SeedGrade(intermediate.Id, 5, sampleVideo,
            ("Дроби", new[] { "Събиране", "Десетични", "Преобразуване" }),
            ("Уравнения", new[] { "Неизвестно", "Прости", "Проверка" }),
            ("Геометрия", new[] { "Ъгли", "Триъгълник", "Лице" }));
        await SeedGrade(intermediate.Id, 6, sampleVideo,
            ("Проценти", new[] { "Отношение", "Пропорция", "Проценти" }),
            ("Алгебра", new[] { "Изрази", "Опростяване", "Уравнения" }),
            ("Геометрия", new[] { "Окръжност", "Лице кръг", "Координати" }));
        await SeedGrade(intermediate.Id, 7, sampleVideo,
            ("Рационални", new[] { "Отрицателни", "Смятане", "Ос" }),
            ("Уравнения", new[] { "Линейни", "Неравенства", "Текст" }),
            ("Геометрия", new[] { "Питагор", "Подобие", "Многоъгълници" }));

        await SeedGrade(advanced.Id, 8, sampleVideo,
            ("Алгебра", new[] { "Степени", "Многочлени", "Формули" }),
            ("Функции", new[] { "Увод", "Линейна", "Графика" }),
            ("Геометрия", new[] { "Вектори", "Окръжност", "Лица" }));
        await SeedGrade(advanced.Id, 9, sampleVideo,
            ("Квадратни", new[] { "Дискриминанта", "Корени", "Задачи" }),
            ("Функции", new[] { "Квадратна", "Графики", "Трансформации" }),
            ("Планиметрия", new[] { "Подобие", "Тригонометрия", "Лица" }));
        await SeedGrade(advanced.Id, 10, sampleVideo,
            ("Логаритми", new[] { "Показателни", "Лог", "Свойства" }),
            ("Тригонометрия", new[] { "sin cos", "Тъждества", "Уравнения" }),
            ("Стереометрия", new[] { "Призма", "Пирамида", "Обем" }));
        await SeedGrade(advanced.Id, 11, sampleVideo,
            ("Комбинаторика", new[] { "Пермутации", "Комбинации", "Задачи" }),
            ("Вероятности", new[] { "Класическа", "Събития", "Практика" }),
            ("Стереометрия", new[] { "Цилиндър", "Конус", "Сфера" }));
        await SeedGrade(advanced.Id, 12, sampleVideo,
            ("Матура", new[] { "Алгебра", "Функции", "Геометрия" }),
            ("Параметър", new[] { "Уравнения", "Неравенства", "Графики" }),
            ("Смесени", new[] { "Задачи 1", "Задачи 2", "Пробен" }));

        await SeedTopic(college.Id, "Part 1 — Граници", 1, sampleVideo, "Граница на редица", "Граница на функция", "Непрекъснатост");
        await SeedTopic(college.Id, "Part 1 — Производни", 2, sampleVideo, "Дефиниция", "Правила", "Приложения");
        await SeedTopic(college.Id, "Part 2 — Матрици", 3, sampleVideo, "Операции", "Детерминанти", "Обратна матрица");
        await SeedTopic(college.Id, "Part 2 — Вектори", 4, sampleVideo, "Независимост", "Базис", "Изображения");
        await SeedTopic(college.Id, "Part 3 — Вероятности", 5, sampleVideo, "Дискретни", "Очакване", "Нормално");
        await SeedTopic(college.Id, "Part 3 — Статистика", 6, sampleVideo, "Описателна", "Извадки", "Интервали");

        TempData["Success"] = "Demo courses loaded.";
        return RedirectToAction(nameof(Index));
    }

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
            _ctx.Lessons.Add(new Lesson
            {
                LevelId = levelId,
                TopicId = topic.Id,
                Title = title,
                Content = $"Урок: {title}",
                Video = video,
                SortOrder = lo++,
                DurationMinutes = 12,
                PointsReward = 20
            });
        }
        await _ctx.SaveChangesAsync();
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
