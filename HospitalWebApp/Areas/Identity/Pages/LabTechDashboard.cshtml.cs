using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "LabTech")]
    public class LabTechDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public LabTechDashboardModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string FullName { get; set; } = string.Empty;
        public int PendingLabOrders { get; set; }
        public int CompletedLabOrders { get; set; }
        public int LabQueueCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!await _userManager.IsInRoleAsync(user, "LabTech"))
                return RedirectToPage("/Dashboard");

            FullName = user.FullName;
            PendingLabOrders = await _context.LabOrders.CountAsync(o => o.Status == "Ordered");
            CompletedLabOrders = await _context.LabOrders.CountAsync(o => o.Status == "Completed");
            LabQueueCount = await _context.QueueEntries.CountAsync(q => q.QueueType == "Lab" && q.Status == "Waiting");
            return Page();
        }
    }
}
