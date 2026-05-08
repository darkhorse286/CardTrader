using System.ComponentModel.DataAnnotations;
using CardTrader.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardTrader.Web.Pages.Account;

public sealed class LoginModel(SignInManager<CardTraderUser> signInManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var result = await signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }

    public sealed class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }
}
