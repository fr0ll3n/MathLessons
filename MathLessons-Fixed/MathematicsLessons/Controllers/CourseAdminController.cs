using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

/// <summary>
/// CRUD for the math catalog (study.com / Khan style):
/// Course (Level) → Section (Topic, e.g. Algebra, Geometry, 1st grade) → Lesson (video) → Exercises after video.
/// Only Owner (Admin) and Teachers.
/// Notifications fire only on content create/update (new course, section, lesson fix).
/// </summary>
[Authorize(Roles = "Teacher,Admin")]
public class CourseAdminController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly INotificationService _notify;
    private readonly IWebHostEnvironment _env;

    public CourseAdminController(ApplicationDbContext ctx, INotificationService notify, IWebHostEnvironment env)
    {
        _ctx = ctx;
        _notify = notify;
        _env = env;
    }

    // ========== COURSES (Level) ==========

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var courses = await _ctx.Levels
            .Include(l => l.Topics)
            .Include(l => l.Lessons)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _ctx.Levels
            .Include(l => l.Topics.OrderBy(t => t.SortOrder))
            .ThenInclude(t => t.Lessons.OrderBy(x => x.SortOrder))
            .ThenInclude(lesson => lesson.Exercises)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (course == null) return NotFound();
        return View(course);
    }

    [HttpGet]
    public IActionResult Create() => View(new Level { SortOrder = 1, BadgeColor = "primary" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Level model)
    {
        if (!ModelState.IsValid) return View(model);
        _ctx.Levels.Add(model);
        await _ctx.SaveChangesAsync();
        await _notify.NotifySubscribersAboutContentAsync(
            "New course available",
            $"{model.Name} ({model.GradeRange}) was added to the catalog.",
            $"/Classroom");
        TempData["Success"] = "Course created.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _ctx.Levels.FindAsync(id);
        if (course == null) return NotFound();
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Level model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        _ctx.Update(model);
        await _ctx.SaveChangesAsync();
        await _notify.NotifySubscribersAboutContentAsync(
            "Course updated",
            $"{model.Name} was updated / fixed.",
            $"/Classroom");
        TempData["Success"] = "Course saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _ctx.Levels
            .Include(l => l.Topics).Include(l => l.Lessons)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (course == null) return NotFound();
        return View(course);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var course = await _ctx.Levels.FindAsync(id);
        if (course != null)
        {
            _ctx.Levels.Remove(course);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Course deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ========== SECTIONS (Topic) ==========

    [HttpGet]
    public async Task<IActionResult> Sections(int courseId)
    {
        var course = await _ctx.Levels.FindAsync(courseId);
        if (course == null) return NotFound();
        ViewBag.Course = course;
        var sections = await _ctx.Topics
            .Include(t => t.Lessons)
            .Where(t => t.LevelId == courseId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
        return View(sections);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSection(int courseId)
    {
        var course = await _ctx.Levels.FindAsync(courseId);
        if (course == null) return NotFound();
        ViewBag.Course = course;
        return View(new Topic { LevelId = courseId, SortOrder = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSection(Topic model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Course = await _ctx.Levels.FindAsync(model.LevelId);
            return View(model);
        }
        _ctx.Topics.Add(model);
        await _ctx.SaveChangesAsync();
        var course = await _ctx.Levels.FindAsync(model.LevelId);
        await _notify.NotifySubscribersAboutContentAsync(
            "New section",
            $"Section \"{model.Name}\" was added to {course?.Name}.",
            $"/Classroom");
        TempData["Success"] = "Section created.";
        return RedirectToAction(nameof(Sections), new { courseId = model.LevelId });
    }

    [HttpGet]
    public async Task<IActionResult> EditSection(int id)
    {
        var section = await _ctx.Topics.Include(t => t.Level).FirstOrDefaultAsync(t => t.Id == id);
        if (section == null) return NotFound();
        ViewBag.Course = section.Level;
        return View(section);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSection(int id, Topic model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Course = await _ctx.Levels.FindAsync(model.LevelId);
            return View(model);
        }
        _ctx.Update(model);
        await _ctx.SaveChangesAsync();
        await _notify.NotifySubscribersAboutContentAsync(
            "Section updated",
            $"Section \"{model.Name}\" was updated.",
            $"/Classroom");
        TempData["Success"] = "Section saved.";
        return RedirectToAction(nameof(Sections), new { courseId = model.LevelId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSection(int id)
    {
        var section = await _ctx.Topics.FindAsync(id);
        if (section == null) return NotFound();
        var courseId = section.LevelId;
        _ctx.Topics.Remove(section);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Section deleted.";
        return RedirectToAction(nameof(Sections), new { courseId });
    }

    // ========== LESSONS ==========

    [HttpGet]
    public async Task<IActionResult> Lessons(int sectionId)
    {
        var section = await _ctx.Topics.Include(t => t.Level).FirstOrDefaultAsync(t => t.Id == sectionId);
        if (section == null) return NotFound();
        ViewBag.Section = section;
        ViewBag.Course = section.Level;
        var lessons = await _ctx.Lessons
            .Include(l => l.Exercises)
            .Where(l => l.TopicId == sectionId)
            .OrderBy(l => l.SortOrder)
            .ToListAsync();
        return View(lessons);
    }

    [HttpGet]
    public async Task<IActionResult> CreateLesson(int sectionId)
    {
        var section = await _ctx.Topics.Include(t => t.Level).FirstOrDefaultAsync(t => t.Id == sectionId);
        if (section == null) return NotFound();
        ViewBag.Section = section;
        return View(new Lesson
        {
            TopicId = sectionId,
            LevelId = section.LevelId,
            SortOrder = 1,
            DurationMinutes = 15,
            PointsReward = 25,
            Content = ""
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLesson(Lesson model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Section = await _ctx.Topics.Include(t => t.Level).FirstAsync(t => t.Id == model.TopicId);
            return View(model);
        }
        var section = await _ctx.Topics.FindAsync(model.TopicId);
        if (section != null) model.LevelId = section.LevelId;
        _ctx.Lessons.Add(model);
        await _ctx.SaveChangesAsync();
        await _notify.NotifySubscribersAboutContentAsync(
            "New lesson",
            $"Lesson \"{model.Title}\" was published.",
            $"/Classroom/Lesson/{model.Id}");
        TempData["Success"] = "Lesson created. Add exercises after the video next.";
        return RedirectToAction(nameof(Exercises), new { lessonId = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditLesson(int id)
    {
        var lesson = await _ctx.Lessons.Include(l => l.Topic).ThenInclude(t => t.Level)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();
        ViewBag.Section = lesson.Topic;
        return View(lesson);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2147483648)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2147483648)]
    public async Task<IActionResult> EditLesson(int id, Lesson model, IFormFile? videoFile)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Section = await _ctx.Topics.Include(t => t.Level).FirstAsync(t => t.Id == model.TopicId);
            return View(model);
        }

        var existing = await _ctx.Lessons.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Title = model.Title;
        existing.Content = model.Content;
        existing.SortOrder = model.SortOrder;
        existing.DurationMinutes = model.DurationMinutes;
        existing.PointsReward = model.PointsReward;
        existing.TopicId = model.TopicId;
        var section = await _ctx.Topics.FindAsync(model.TopicId);
        if (section != null) existing.LevelId = section.LevelId;

        if (videoFile != null && videoFile.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "uploads", "lessons");
            Directory.CreateDirectory(dir);
            var name = $"{id}_{Guid.NewGuid():N}{Path.GetExtension(videoFile.FileName)}";
            await using (var fs = System.IO.File.Create(Path.Combine(dir, name)))
                await videoFile.CopyToAsync(fs);
            existing.Video = $"/uploads/lessons/{name}";
        }

        await _ctx.SaveChangesAsync();
        await _notify.NotifySubscribersAboutContentAsync(
            "Lesson updated",
            $"\"{existing.Title}\" was fixed / updated.",
            $"/Classroom/Lesson/{id}");
        TempData["Success"] = "Lesson saved.";
        return RedirectToAction(nameof(Lessons), new { sectionId = existing.TopicId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLesson(int id)
    {
        var lesson = await _ctx.Lessons.FindAsync(id);
        if (lesson == null) return NotFound();
        var sectionId = lesson.TopicId;
        _ctx.Lessons.Remove(lesson);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Lesson deleted.";
        return RedirectToAction(nameof(Lessons), new { sectionId });
    }

    // ========== EXERCISES (after video) ==========

    [HttpGet]
    public async Task<IActionResult> Exercises(int lessonId)
    {
        var lesson = await _ctx.Lessons
            .Include(l => l.Topic).ThenInclude(t => t.Level)
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null) return NotFound();
        ViewBag.Lesson = lesson;
        return View(lesson.Exercises.ToList());
    }

    [HttpGet]
    public async Task<IActionResult> CreateExercise(int lessonId)
    {
        var lesson = await _ctx.Lessons.FindAsync(lessonId);
        if (lesson == null) return NotFound();
        ViewBag.Lesson = lesson;
        return View(new Exercise { LessonId = lessonId, PointsReward = 10 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExercise(Exercise model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Lesson = await _ctx.Lessons.FindAsync(model.LessonId);
            return View(model);
        }
        _ctx.Exercises.Add(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Exercise added (shown after the video).";
        return RedirectToAction(nameof(Exercises), new { lessonId = model.LessonId });
    }

    [HttpGet]
    public async Task<IActionResult> EditExercise(int id)
    {
        var ex = await _ctx.Exercises.Include(e => e.Lesson).FirstOrDefaultAsync(e => e.Id == id);
        if (ex == null) return NotFound();
        ViewBag.Lesson = ex.Lesson;
        return View(ex);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditExercise(int id, Exercise model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Lesson = await _ctx.Lessons.FindAsync(model.LessonId);
            return View(model);
        }
        _ctx.Update(model);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Exercise saved.";
        return RedirectToAction(nameof(Exercises), new { lessonId = model.LessonId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var ex = await _ctx.Exercises.FindAsync(id);
        if (ex == null) return NotFound();
        var lessonId = ex.LessonId;
        _ctx.Exercises.Remove(ex);
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Exercise deleted.";
        return RedirectToAction(nameof(Exercises), new { lessonId });
    }
}
