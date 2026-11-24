using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Pharmacy")]
    public class PharmacyDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PharmacyDashboardModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string FullName { get; set; } = string.Empty;
        public int PharmacyQueueCount { get; set; }
        public int PendingOrders { get; set; }
        public int ReadyForCollectionOrders { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!await _userManager.IsInRoleAsync(user, "Pharmacy"))
                return RedirectToPage("/Dashboard");

            FullName = user.FullName;
            PharmacyQueueCount = await _context.QueueEntries.CountAsync(q => q.QueueType == "Pharmacy" && q.Status == "Waiting");
            PendingOrders = await _context.PharmacyOrders.CountAsync(order => order.Status == PharmacyOrder.PendingReview);
            ReadyForCollectionOrders = await _context.PharmacyOrders.CountAsync(order => order.Status == PharmacyOrder.ReadyForCollection);
            return Page();
        }
    }
}
