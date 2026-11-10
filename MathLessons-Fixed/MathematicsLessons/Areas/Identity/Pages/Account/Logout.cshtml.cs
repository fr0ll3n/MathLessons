using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MathematicsLessons.Areas.Identity.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    public LogoutModel(SignInManager<User> signInManager) => _signInManager = signInManager;

    public async Task<IActionResult> OnPost(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        return returnUrl != null ? LocalRedirect(returnUrl) : RedirectToPage("/Index", new { area = "" });
    }
}
