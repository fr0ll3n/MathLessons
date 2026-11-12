using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Controllers;

public class ExercisesController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<User> _users;
    private readonly IPointsService _points;

    public ExercisesController(ApplicationDbContext ctx, UserManager<User> users, IPointsService points)
    {
        _ctx = ctx; _users = users; _points = points;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Practice(int id)
    {
        var exercise = await _ctx.Exercises.Include(e => e.Lesson).FirstOrDefaultAsync(e => e.Id == id);
        if (exercise == null) return NotFound();
        return View(exercise);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Submit(int id, string studentAnswer)
    {
        var exercise = await _ctx.Exercises.Include(e => e.Lesson).FirstOrDefaultAsync(e => e.Id == id);
        if (exercise == null) return NotFound();

        var ok = AnswerChecker.IsCorrect(exercise.Solution, studentAnswer);

        if (ok)
        {
            var me = await _users.GetUserAsync(User);
            if (me != null)
            {
                // Only pay out the first time this lesson's exercises are passed, so the
                // form cannot simply be resubmitted to farm points.
                var progress = await _ctx.LessonProgress
                    .FirstOrDefaultAsync(p => p.UserId == me.Id && p.LessonId == exercise.LessonId);
                if (progress?.ExercisesPassed != true)
                    await _points.AwardAsync(me, exercise.PointsReward, "Exercise correct");
            }
            ViewBag.IsCorrect = true;
            ViewBag.FeedbackMessage = $"Correct! +{exercise.PointsReward} points.";
        }
        else
        {
            ViewBag.IsCorrect = false;
            ViewBag.FeedbackMessage = exercise.Hint ?? "Not quite right.";
        }
        return View("Feedback", exercise);
    }
}
