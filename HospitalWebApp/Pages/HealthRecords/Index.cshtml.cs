using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.HealthRecords
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<HealthRecord> HealthRecords { get; set; } = new List<HealthRecord>();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Admin") || User.IsInRole("Doctor"))
            {
                HealthRecords = await _context.HealthRecords
                    .Include(hr => hr.Patient)
                    .OrderByDescending(hr => hr.RecordDate)
                    .ToListAsync();
            }
            else if (!string.IsNullOrEmpty(userId))
            {
                HealthRecords = await _context.HealthRecords
                    .Include(hr => hr.Patient)
                    .Where(hr => hr.PatientId == userId)
                    .OrderByDescending(hr => hr.RecordDate)
                    .ToListAsync();
            }
            else
            {
                HealthRecords = new List<HealthRecord>();
            }
        }
    }
}
