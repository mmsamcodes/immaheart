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

        public ManageStaffModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IList<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public IList<string> Roles { get; set; } = new List<string>();
        public IDictionary<string, string> CurrentRoles { get; set; } = new Dictionary<string, string>();
        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadPageAsync();
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
