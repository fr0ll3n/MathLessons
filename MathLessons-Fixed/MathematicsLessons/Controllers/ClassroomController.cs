using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Models;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

/// <summary>Course path: levels → topics → lessons → progress (Khan-style unlock).</summary>
[Authorize]
public class ClassroomController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;
    private readonly IWebHostEnvironment _env;
    private readonly ILessonAccessService _access;
    private readonly IProgressService _progress;
    private readonly ICertificateService _certs;
    private readonly IPointsService _points;
    private readonly INotificationService _notify;
    private readonly ISubscriptionService _subs;

    public ClassroomController(
        ApplicationDbContext ctx, UserManager<User> users, IWebHostEnvironment env,
        ILessonAccessService access, IProgressService progress, ICertificateService certs,
        IPointsService points, INotificationService notify, ISubscriptionService subs)
    {
        _ctx = ctx; _users = users; _env = env;
        _access = access; _progress = progress; _certs = certs;
        _points = points; _notify = notify; _subs = subs;
    }

    private async Task<User?> MeAsync()
    {
        var me = await _users.GetUserAsync(User);
        if (me == null) return null;
        return await _ctx.Users.Include(u => u.Sub).FirstAsync(u => u.Id == me.Id);
    }

    private bool IsStaff() => User.IsInRole("Teacher") || User.IsInRole("Admin");

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var me = await MeAsync();
        /* Free users can browse; only first Beginner lesson is unlocked */

        var levels = await _ctx.Levels.OrderBy(l => l.SortOrder).ToListAsync();
        var topics = await _ctx.Topics.OrderBy(t => t.SortOrder).ToListAsync();
        var lessons = await _ctx.Lessons.OrderBy(l => l.SortOrder).ToListAsync();
        var completed = me == null ? new HashSet<int>() : await _progress.GetCompletedLessonIdsAsync(me.Id);
        var certs = me == null
            ? new HashSet<int>()
            : (await _ctx.Certificates.Where(c => c.UserId == me.Id).Select(c => c.LevelId).ToListAsync()).ToHashSet();

        var isSub = me != null && _subs.IsSubscribed(me);
        var isStaff = IsStaff();
        var freeFirstLessonId = 0;
        if (!isSub && !isStaff)
        {
            var begId = levels.OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
            freeFirstLessonId = lessons.Where(l => l.LevelId == begId).OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
        }

        var page = new ClassroomPage
        {
            IsSubscribed = isSub || isStaff,
            TotalCompleted = completed.Count,
            TotalLessons = lessons.Count
        };

        foreach (var level in levels)
        {
            var levelLessons = lessons.Where(l => l.LevelId == level.Id).ToList();
            var block = new LevelBlock
            {
                Level = level,
                IsUnlocked = isStaff || isSub
                    ? _access.IsLevelUnlocked(level, levels, lessons, completed)
                    : levels.OrderBy(l => l.SortOrder).Select(x => x.Id).FirstOrDefault() == level.Id,
                Completed = levelLessons.Count(l => completed.Contains(l.Id)),
                Total = levelLessons.Count,
                HasCertificate = certs.Contains(level.Id)
            };
            foreach (var topic in topics.Where(t => t.LevelId == level.Id))
            {
                var tb = new TopicBlock { Topic = topic };
                foreach (var lesson in levelLessons.Where(l => l.TopicId == topic.Id).OrderBy(l => l.SortOrder))
                {
                    bool lessonUnlocked;
                    if (isStaff || isSub)
                        lessonUnlocked = _access.IsLessonUnlocked(lesson, levels, lessons, completed);
                    else
                        lessonUnlocked = freeFirstLessonId != 0 && lesson.Id == freeFirstLessonId;
                    tb.Lessons.Add(new LessonRow
                    {
                        Lesson = lesson,
                        IsUnlocked = lessonUnlocked,
                        IsCompleted = completed.Contains(lesson.Id)
                    });
                }
                block.Topics.Add(tb);
            }
            page.Levels.Add(block);
        }
        return View(page);
    }


    [HttpGet]
    public async Task<IActionResult> Level(int id)
    {
        var me = await MeAsync();
        var levels = await _ctx.Levels.OrderBy(l => l.SortOrder).ToListAsync();
        var level = levels.FirstOrDefault(l => l.Id == id);
        if (level == null) return NotFound();

        var lessons = await _ctx.Lessons.OrderBy(l => l.SortOrder).ToListAsync();
        var topics = await _ctx.Topics.Where(t => t.LevelId == id).OrderBy(t => t.SortOrder).ToListAsync();
        var completed = me == null ? new HashSet<int>() : await _progress.GetCompletedLessonIdsAsync(me.Id);
        var isSub = me != null && _subs.IsSubscribed(me);
        var isStaff = IsStaff();

        var freeFirstId = 0;
        if (!isSub && !isStaff)
        {
            var begId = levels.OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
            freeFirstId = lessons.Where(l => l.LevelId == begId).OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
        }

        var levelUnlocked = isStaff || isSub
            ? _access.IsLevelUnlocked(level, levels, lessons, completed)
            : levels.OrderBy(l => l.SortOrder).Select(x => x.Id).FirstOrDefault() == level.Id;

        var levelLessons = lessons.Where(l => l.LevelId == id).ToList();
        var block = new LevelBlock
        {
            Level = level,
            IsUnlocked = levelUnlocked,
            Completed = levelLessons.Count(l => completed.Contains(l.Id)),
            Total = levelLessons.Count,
            HasCertificate = me != null && await _ctx.Certificates.AnyAsync(c => c.UserId == me.Id && c.LevelId == id)
        };
        foreach (var topic in topics)
        {
            var tb = new TopicBlock { Topic = topic };
            foreach (var lesson in levelLessons.Where(l => l.TopicId == topic.Id).OrderBy(l => l.SortOrder))
            {
                bool unlocked;
                if (isStaff || isSub)
                    unlocked = _access.IsLessonUnlocked(lesson, levels, lessons, completed);
                else
                    unlocked = freeFirstId != 0 && lesson.Id == freeFirstId;
                tb.Lessons.Add(new LessonRow
                {
                    Lesson = lesson,
                    IsUnlocked = unlocked,
                    IsCompleted = completed.Contains(lesson.Id)
                });
            }
            block.Topics.Add(tb);
        }
        ViewBag.IsSubscribed = isSub || isStaff;
        return View(block);
    }

    [HttpGet]
    public async Task<IActionResult> Lesson(int id)
    {
        var me = await MeAsync();
        var lesson = await _ctx.Lessons
            .Include(l => l.Topic).Include(l => l.Level).Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();

        var levels = await _ctx.Levels.OrderBy(l => l.SortOrder).ToListAsync();
        var lessons = await _ctx.Lessons.OrderBy(l => l.SortOrder).ToListAsync();
        var completed = me == null ? new HashSet<int>() : await _progress.GetCompletedLessonIdsAsync(me.Id);

        // Free users: only first Beginner lesson. Math Enthusiast/Staff: normal path.
        bool unlocked;
        if (IsStaff() || (me != null && _subs.IsSubscribed(me)))
        {
            unlocked = _access.IsLessonUnlocked(lesson, levels, lessons, completed) || IsStaff();
        }
        else
        {
            var beginnerId = levels.OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
            var firstId = lessons.Where(l => l.LevelId == beginnerId).OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
            unlocked = lesson.Id == firstId;
            if (!unlocked && me != null)
            {
                TempData["Error"] = "Free планът включва само първия Beginner урок. Стани Math Enthusiast за всички курсове.";
                return RedirectToAction("Plans", "Subscription");
            }
        }

        LessonProgress? prog = null;
        if (me != null)
            prog = await _ctx.LessonProgress.FirstOrDefaultAsync(p => p.UserId == me.Id && p.LessonId == id);

        var next = lessons
            .Where(l => l.LevelId == lesson.LevelId && l.SortOrder > lesson.SortOrder)
            .OrderBy(l => l.SortOrder).FirstOrDefault();

        var watched = prog?.VideoWatched ?? false;
        var passed = prog?.ExercisesPassed ?? false;
        return View(new LessonPage
        {
            Lesson = lesson,
            IsUnlocked = unlocked,
            IsCompleted = prog?.IsCompleted ?? false,
            WatchedPercent = prog?.WatchedPercent ?? 0,
            NextLesson = next,
            CanUpload = IsStaff(),
            VideoWatched = watched,
            ExercisesPassed = passed,
            CanPractice = watched && !passed,
            Exercises = lesson.Exercises.OrderBy(e => e.Id).ToList()
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Progress(int id, int percent)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var lesson = await _ctx.Lessons.FindAsync(id);
        if (lesson == null) return NotFound();

        var levels = await _ctx.Levels.OrderBy(l => l.SortOrder).ToListAsync();
        var lessons = await _ctx.Lessons.OrderBy(l => l.SortOrder).ToListAsync();
        var completed = await _progress.GetCompletedLessonIdsAsync(me.Id);

        bool canProgress;
        if (IsStaff() || _subs.IsSubscribed(me))
            canProgress = _access.IsLessonUnlocked(lesson, levels, lessons, completed) || IsStaff();
        else
        {
            var begId = levels.OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
            var firstId = lessons.Where(l => l.LevelId == begId).OrderBy(l => l.SortOrder).Select(l => l.Id).FirstOrDefault();
            canProgress = lesson.Id == firstId;
        }
        if (!canProgress) return Forbid();

        var wasDone = completed.Contains(id);
        var row = await _progress.RecordWatchAsync(me.Id, id, percent);
        if (row.IsCompleted && !wasDone)
        {
            await _certs.TryIssueForLevelAsync(me.Id, lesson.LevelId);
        }
        return RedirectToAction(nameof(Lesson), new { id });
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(107374182400)]
    [RequestFormLimits(MultipartBodyLengthLimit = 107374182400)]
    public async Task<IActionResult> Upload(int id, IFormFile video)
    {
        var lesson = await _ctx.Lessons.FindAsync(id);
        if (lesson == null || video == null || video.Length == 0) return NotFound();

        var dir = Path.Combine(_env.WebRootPath, "uploads", "lessons");
        Directory.CreateDirectory(dir);
        var name = $"{id}_{Guid.NewGuid():N}{Path.GetExtension(video.FileName)}";
        var path = Path.Combine(dir, name);
        await using (var fs = System.IO.File.Create(path))
            await video.CopyToAsync(fs);
        lesson.Video = $"/uploads/lessons/{name}";
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Video uploaded.";
        return RedirectToAction(nameof(Lesson), new { id });
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveVideo(int id)
    {
        var lesson = await _ctx.Lessons.FindAsync(id);
        if (lesson == null) return NotFound();
        if (!string.IsNullOrEmpty(lesson.Video) && lesson.Video.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var physical = Path.Combine(_env.WebRootPath, lesson.Video.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);
        }
        lesson.Video = null;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Video removed. You can upload a new one.";
        return RedirectToAction(nameof(Lesson), new { id });
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkVideoWatched(int id)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var lesson = await _ctx.Lessons.FindAsync(id);
        if (lesson == null) return NotFound();
        var progress = await _ctx.LessonProgress.FirstOrDefaultAsync(p => p.UserId == me.Id && p.LessonId == id);
        if (progress == null)
        {
            progress = new LessonProgress { UserId = me.Id, LessonId = id, VideoWatched = true, WatchedPercent = 100 };
            _ctx.LessonProgress.Add(progress);
        }
        else
        {
            progress.VideoWatched = true;
            progress.WatchedPercent = 100;
            progress.UpdatedAt = DateTime.UtcNow;
        }
        await _ctx.SaveChangesAsync();
        return RedirectToAction(nameof(Lesson), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExercises(int id, List<string> answers)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();
        var lesson = await _ctx.Lessons.Include(l => l.Exercises).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();
        var progress = await _ctx.LessonProgress.FirstOrDefaultAsync(p => p.UserId == me.Id && p.LessonId == id);
        if (progress == null || !progress.VideoWatched)
        {
            TempData["Error"] = "Watch the full video first.";
            return RedirectToAction(nameof(Lesson), new { id });
        }
        var exercises = lesson.Exercises.OrderBy(e => e.Id).ToList();
        if (exercises.Count == 0)
        {
            progress.ExercisesPassed = true;
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
            TempData["Success"] = "Lesson complete. Next lesson unlocked.";
            return RedirectToAction(nameof(Lesson), new { id });
        }
        bool allOk = true;
        for (int i = 0; i < exercises.Count; i++)
        {
            var expected = (exercises[i].Solution ?? "").Replace(" ", "").ToLowerInvariant();
            var given = (answers != null && i < answers.Count ? answers[i] : "").Replace(" ", "").ToLowerInvariant();
            if (expected != "practice" && expected != given && !given.Contains(expected))
                allOk = false;
        }
        if (!allOk)
        {
            progress.VideoWatched = false;
            progress.ExercisesPassed = false;
            progress.IsCompleted = false;
            progress.WatchedPercent = 0;
            await _ctx.SaveChangesAsync();
            TempData["Error"] = "Wrong answers. Watch the video again, then retry.";
            return RedirectToAction(nameof(Lesson), new { id });
        }
        progress.ExercisesPassed = true;
        progress.IsCompleted = true;
        progress.CompletedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Exercises passed. Next lesson unlocked.";
        return RedirectToAction(nameof(Lesson), new { id });
    }

    public async Task<IActionResult> Practice(int id, string answer)
    {
        var me = await MeAsync();
        if (me == null) return Challenge();

        var exercise = await _ctx.Exercises.Include(e => e.Lesson).FirstOrDefaultAsync(e => e.LessonId == id)
                       ?? await _ctx.Exercises.Include(e => e.Lesson).FirstOrDefaultAsync(e => e.Id == id);
        if (exercise == null)
        {
            TempData["Error"] = "No exercise for this lesson.";
            return RedirectToAction(nameof(Lesson), new { id });
        }

        var expected = exercise.Solution.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        var given = (answer ?? "").Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        var ok = expected.Equals(given, StringComparison.OrdinalIgnoreCase)
                 || given.Contains(expected, StringComparison.OrdinalIgnoreCase)
                 || expected == "practice";

        if (ok)
            await _points.AwardAsync(me, exercise.PointsReward, "Exercise correct");

        return View("Lesson", new LessonPage
        {
            Lesson = await _ctx.Lessons.Include(l => l.Exercises).Include(l => l.Topic).Include(l => l.Level)
                .FirstAsync(l => l.Id == exercise.LessonId),
            IsUnlocked = true,
            PracticeCorrect = ok,
            PracticeMessage = ok ? $"Correct! +{exercise.PointsReward} points." : (exercise.Hint ?? "Try again.")
        });
    }
}
