using System.ComponentModel.DataAnnotations;
using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MathematicsLessons.Areas.Identity.Pages.Account.Manage;

public class ChangePasswordModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public ChangePasswordModel(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = "";

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user == null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return Page();
        }
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Password changed.";
        return RedirectToPage();
    }
}
