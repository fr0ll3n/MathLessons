using System.ComponentModel.DataAnnotations;
using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MathematicsLessons.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public LoginModel(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Display(Name = "Парола")]
        public string Password { get; set; } = "";

        [Display(Name = "Запомни ме")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        if (!ModelState.IsValid) return Page();

        // Find by email first (more reliable than treating email as userName only)
        var user = await _userManager.FindByEmailAsync(Input.Email.Trim());
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Грешен email или парола. Ако нямаш акаунт — регистрирай се.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? user.Email!,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
            return LocalRedirect(returnUrl);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Акаунтът е временно заключен. Опитай по-късно.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Грешен email или парола.");
        return Page();
    }
}
