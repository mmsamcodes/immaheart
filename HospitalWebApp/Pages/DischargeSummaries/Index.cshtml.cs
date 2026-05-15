using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.DischargeSummaries
{
    [Authorize(Roles = "Admin,Doctor,Nurse,Accounts,Cashier,Patient")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<DischargeDocument> Documents { get; set; } = new List<DischargeDocument>();

        public async Task OnGetAsync()
        {
            if (User.IsInRole("Patient"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Documents = await _context.DischargeDocuments
                    .Where(d => d.PatientId == userId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
            }
            else
            {
                Documents = await _context.DischargeDocuments
                    .Include(d => d.Patient)
                    .Include(d => d.UploadedBy)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();
            }
        }
    }
}
