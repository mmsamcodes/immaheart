using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HospitalWebApp.Pages.Queue
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string QueueType { get; set; } = string.Empty;
        public IList<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();

        public async Task<IActionResult> OnGetAsync(string queueType)
        {
            QueueType = queueType?.Trim() ?? string.Empty;
            var query = _context.QueueEntries
                .Include(q => q.Patient)
                .Include(q => q.AssignedBy)
                .AsQueryable();

            if (User.IsInRole("Admin"))
            {
                if (!string.IsNullOrEmpty(QueueType))
                {
                    query = query.Where(q => q.QueueType == QueueType);
                }
            }
            else if (User.IsInRole("Accounts") || User.IsInRole("Cashier"))
            {
                return Forbid();
            }
            else if (User.IsInRole("Doctor") || User.IsInRole("Nurse") ||
                     User.IsInRole("LabTech") || User.IsInRole("Pharmacy") ||
                     User.IsInRole("Reception"))
            {
                var allowedQueues = User.IsInRole("Doctor") ? new[] { "Doctor" }
                    : User.IsInRole("Nurse") ? new[] { "Doctor", "Lab", "Pharmacy", "Triage" }
                    : User.IsInRole("LabTech") ? new[] { "Lab" }
                    : User.IsInRole("Pharmacy") ? new[] { "Pharmacy" }
                    : new[] { "Triage" };

                if (!string.IsNullOrEmpty(QueueType) && !allowedQueues.Contains(QueueType))
                {
                    return Forbid();
                }

                query = query.Where(q => allowedQueues.Contains(q.QueueType));
                if (User.IsInRole("Doctor"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    query = query.Where(q => q.AssignedDoctorId == null || q.AssignedDoctorId == userId);
                }
                if (!string.IsNullOrEmpty(QueueType))
                {
                    query = query.Where(q => q.QueueType == QueueType);
                }
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(q => q.PatientId == userId);
                if (!string.IsNullOrEmpty(QueueType))
                {
                    query = query.Where(q => q.QueueType == QueueType);
                }
            }

            QueueEntries = await query.OrderBy(q => q.CreatedAt).ToListAsync();
            return Page();
        }
    }
}
