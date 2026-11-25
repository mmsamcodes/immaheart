using System.ComponentModel.DataAnnotations;
using System.Text;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace HospitalWebApp.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class AcceptInvitationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AcceptInvitationModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? userId, string? code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest();
        }

        Input.UserId = userId;
        Input.Code = code;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var staff = await _userManager.FindByIdAsync(Input.UserId);
        if (staff == null || staff.EmailConfirmed)
        {
            return BadRequest();
        }

        string resetToken;
        try
        {
            resetToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        }
        catch (FormatException)
        {
            return BadRequest();
        }

        var passwordResult = await _userManager.ResetPasswordAsync(staff, resetToken, Input.Password);
        if (!passwordResult.Succeeded)
        {
            foreach (var error in passwordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        staff.EmailConfirmed = true;
        var confirmationResult = await _userManager.UpdateAsync(staff);
        if (!confirmationResult.Succeeded)
        {
            foreach (var error in confirmationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}
