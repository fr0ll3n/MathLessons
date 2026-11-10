using System.ComponentModel.DataAnnotations;
using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MathematicsLessons.Areas.Identity.Pages.Account.Manage;

public class IndexModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public IndexModel(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string Email { get; set; } = "";

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(50)]
        public string LastName { get; set; } = "";

        [Phone]
        public string? PhoneNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        Email = user.Email ?? "";
        Input = new InputModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        if (!ModelState.IsValid) return Page();

        user.FirstName = Input.FirstName;
        user.LastName = Input.LastName;
        user.PhoneNumber = Input.PhoneNumber;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Profile updated.";
        return RedirectToPage();
    }
}
