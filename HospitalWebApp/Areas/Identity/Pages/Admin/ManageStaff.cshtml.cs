using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System;

namespace HospitalWebApp.Areas.Identity.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageStaffModel : PageModel
    {
        private static readonly string[] AssignableRoles =
        [
            "Admin", "Doctor", "Accounts", "Cashier", "Reception", "Nurse", "LabTech", "Pharmacy"
        ];

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ManageStaffModel> _logger;

        public ManageStaffModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            ILogger<ManageStaffModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public IList<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public IList<string> Roles { get; set; } = new List<string>();
        public IDictionary<string, string> CurrentRoles { get; set; } = new Dictionary<string, string>();

        [BindProperty]
        public StaffInviteInput Invite { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class StaffInviteInput
        {
            [Required]
            [StringLength(120, MinimumLength = 2)]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        public async Task<IActionResult> OnPostInviteAsync()
        {
            if (Invite == null)
            {
                Invite = new StaffInviteInput();
                ModelState.AddModelError("Invite", "Enter staff invitation details.");
            }

            Invite.FullName = Invite.FullName?.Trim() ?? string.Empty;
            Invite.Email = Invite.Email?.Trim() ?? string.Empty;

            if (!AssignableRoles.Contains(Invite.Role, StringComparer.Ordinal))
            {
                ModelState.AddModelError("Invite.Role", "Select a supported staff role.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPageAsync();
                return Page();
            }

            if (await _userManager.FindByEmailAsync(Invite.Email) != null)
            {
                StatusMessage = "An account already uses this email. Assign its staff role from the staff list.";
                return RedirectToPage();
            }

            if (!await _roleManager.RoleExistsAsync(Invite.Role))
            {
                StatusMessage = "The selected role is not configured. Contact a system administrator.";
                return RedirectToPage();
            }

            var staff = new ApplicationUser
            {
                UserName = Invite.Email,
                Email = Invite.Email,
                FullName = Invite.FullName,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(staff);
            if (!createResult.Succeeded)
            {
                StatusMessage = $"Staff account creation failed: {string.Join("; ", createResult.Errors.Select(error => error.Description))}";
                return RedirectToPage();
            }

            var roleResult = await _userManager.AddToRoleAsync(staff, Invite.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(staff);
                StatusMessage = $"Role assignment failed: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}";
                return RedirectToPage();
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(staff);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
            var inviteUrl = Url.Page(
                "/Account/AcceptInvitation",
                pageHandler: null,
                values: new { area = "Identity", userId = staff.Id, code = encodedToken },
                protocol: Request.Scheme);

            try
            {
                if (string.IsNullOrWhiteSpace(inviteUrl))
                {
                    throw new InvalidOperationException("The staff invitation URL could not be generated.");
                }

                var name = HtmlEncoder.Default.Encode(staff.FullName);
                var roleName = HtmlEncoder.Default.Encode(Invite.Role);
                var message = $"<p>Hello {name},</p>" +
                              $"<p>An account has been created for you at Immaculate Heart Hospital Kereita with the role <strong>{roleName}</strong>.</p>" +
                              $"<p><a href=\"{HtmlEncoder.Default.Encode(inviteUrl)}\">Accept the invitation and set your password</a></p>" +
                              "<p>This link is personal. Do not forward it. If you were not expecting this invitation, contact the hospital administrator.</p>";
                await _emailSender.SendEmailAsync(staff.Email!, "Your hospital staff account invitation", message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not send staff invitation to {Email}.", staff.Email);
                var deleteResult = await _userManager.DeleteAsync(staff);
                if (!deleteResult.Succeeded)
                {
                    _logger.LogError(
                        "Could not remove uninvited staff account {UserId}: {Errors}",
                        staff.Id,
                        string.Join("; ", deleteResult.Errors.Select(error => error.Description)));
                }

                StatusMessage = deleteResult.Succeeded
                    ? "The invitation could not be sent, so the new account was removed. Check email configuration and try again."
                    : "The invitation could not be sent and the account could not be removed. Contact a system administrator.";
                return RedirectToPage();
            }

            StatusMessage = $"Invitation sent to {staff.Email}. The account is inactive until the staff member sets a password.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string userId, string role)
        {
            if (string.IsNullOrWhiteSpace(userId) || !AssignableRoles.Contains(role, StringComparer.Ordinal))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                StatusMessage = "You cannot change your own role.";
                return RedirectToPage();
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                StatusMessage = "The selected role is not configured. Contact a system administrator.";
                return RedirectToPage();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            if (currentRoles.Contains("Admin") && role != "Admin" && admins.Count <= 1)
            {
                StatusMessage = "The last administrator cannot be reassigned.";
                return RedirectToPage();
            }

            if (!currentRoles.Contains(role))
            {
                var addResult = await _userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    StatusMessage = $"Role assignment failed: {string.Join("; ", addResult.Errors.Select(error => error.Description))}";
                    return RedirectToPage();
                }
            }

            foreach (var currentRole in currentRoles.Where(currentRole =>
                         AssignableRoles.Contains(currentRole, StringComparer.Ordinal) || currentRole == "Patient"))
            {
                if (currentRole == role)
                {
                    continue;
                }

                var removeResult = await _userManager.RemoveFromRoleAsync(user, currentRole);
                if (!removeResult.Succeeded)
                {
                    StatusMessage = $"Role update was incomplete: {string.Join("; ", removeResult.Errors.Select(error => error.Description))}";
                    return RedirectToPage();
                }
            }

            StatusMessage = $"{user.FullName}'s role was updated to {role}.";
            return RedirectToPage();
        }

        private async Task LoadPageAsync()
        {
            var configuredRoles = await _roleManager.Roles.Select(existingRole => existingRole.Name).ToListAsync();
            Roles = AssignableRoles.Where(configuredRoles.Contains).ToList();
            Users = await _userManager.Users.OrderBy(user => user.FullName).ToListAsync();
            foreach (var user in Users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var currentRole = userRoles.FirstOrDefault(role => AssignableRoles.Contains(role, StringComparer.Ordinal));
                if (currentRole != null)
                {
                    CurrentRoles[user.Id] = currentRole;
                }
                else if (userRoles.Contains("Patient"))
                {
                    CurrentRoles[user.Id] = "Patient";
                }
                else
                {
                    CurrentRoles[user.Id] = "Unassigned";
                }
            }
        }
    }
}
