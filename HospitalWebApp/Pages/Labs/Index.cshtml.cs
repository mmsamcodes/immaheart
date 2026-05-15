using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Pages_Labs
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<LabOrder> LabOrders { get; set; } = new List<LabOrder>();

        public async Task OnGetAsync()
        {
            LabOrders = await _context.LabOrders
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .ToListAsync();
        }
    }
}
