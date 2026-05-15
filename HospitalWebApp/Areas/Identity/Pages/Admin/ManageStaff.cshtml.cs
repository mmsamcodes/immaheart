using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HospitalWebApp.Areas.Identity.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManageStaffModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ManageStaffModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IList<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public IList<string> Roles { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            Users = _userManager.Users.ToList();
            Roles = _roleManager.Roles.Select(r => r.Name ?? "").ToList();
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Remove existing staff roles (Doctor,Nurse,LabTech,Pharmacy,Accounts,Cashier,Reception)
            var staffRoles = new[] { "Doctor", "Nurse", "LabTech", "Pharmacy", "Accounts", "Cashier", "Reception", "Admin" };
            foreach (var r in staffRoles)
            {
                if (await _userManager.IsInRoleAsync(user, r))
                {
                    await _userManager.RemoveFromRoleAsync(user, r);
                }
            }

            // Add selected role
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
            await _userManager.AddToRoleAsync(user, role);

            return RedirectToPage();
        }
    }
}
