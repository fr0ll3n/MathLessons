using MathematicsLessons.Infrastructure.Data;
using MathematicsLessons.Infrastructure.Data.Entities;
using MathematicsLessons.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Submit(int id, string studentAnswer)
    {
        var exercise = await _ctx.Exercises.Include(e => e.Lesson).FirstOrDefaultAsync(e => e.Id == id);
        if (exercise == null) return NotFound();
        var expected = exercise.Solution.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        var given = (studentAnswer ?? "").Replace(" ", "", StringComparison.OrdinalIgnoreCase);
        var ok = expected.Equals(given, StringComparison.OrdinalIgnoreCase)
                 || given.Contains(expected, StringComparison.OrdinalIgnoreCase)
                 || expected == "practice";
        if (ok)
        {
            var me = await _users.GetUserAsync(User);
            if (me != null) await _points.AwardAsync(me, exercise.PointsReward, "Exercise correct");
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
