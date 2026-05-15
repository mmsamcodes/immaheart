using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.Triage
{
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<TriageEntry> TriageEntries { get; set; } = new List<TriageEntry>();
        public int PendingDoctorCount { get; set; }
        public int PendingLabCount { get; set; }
        public int PendingPharmacyCount { get; set; }

        public async Task OnGetAsync()
        {
            TriageEntries = await _context.TriageEntries
                .Include(t => t.Patient)
                .Include(t => t.Nurse)
                .Include(t => t.AssignedDoctor)
                .OrderByDescending(t => t.TriageDate)
                .ToListAsync();

            PendingDoctorCount = TriageEntries.Count(t => t.Status == "PendingDoctor");
            PendingLabCount = TriageEntries.Count(t => t.Status == "PendingLab");
            PendingPharmacyCount = TriageEntries.Count(t => t.Status == "PendingPharmacy");
        }
    }
}
