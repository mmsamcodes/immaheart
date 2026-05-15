using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Nurse")]
    public class NurseDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public NurseDashboardModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string FullName { get; set; } = string.Empty;
        public int PendingAdmissions { get; set; }
        public int DoctorQueueCount { get; set; }
        public int LabQueueCount { get; set; }
        public int PharmacyQueueCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            if (!await _userManager.IsInRoleAsync(user, "Nurse"))
                return RedirectToPage("/Dashboard");

            FullName = user.FullName;
            PendingAdmissions = await _context.Admissions.CountAsync(a => a.Status == "Admitted");
            DoctorQueueCount = await _context.QueueEntries.CountAsync(q => q.QueueType == "Doctor" && q.Status == "Waiting");
            LabQueueCount = await _context.QueueEntries.CountAsync(q => q.QueueType == "Lab" && q.Status == "Waiting");
            PharmacyQueueCount = await _context.QueueEntries.CountAsync(q => q.QueueType == "Pharmacy" && q.Status == "Waiting");
            return Page();
        }
    }
}
