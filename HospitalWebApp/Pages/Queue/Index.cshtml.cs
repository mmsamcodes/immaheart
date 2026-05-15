using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;
using HospitalWebApp.Models;

namespace HospitalWebApp.Pages.Queue
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string QueueType { get; set; } = string.Empty;
        public IList<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();

        public async Task OnGetAsync(string queueType)
        {
            QueueType = queueType?.Trim() ?? string.Empty;
            var query = _context.QueueEntries
                .Include(q => q.Patient)
                .Include(q => q.AssignedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(QueueType))
            {
                query = query.Where(q => q.QueueType == QueueType);
            }

            QueueEntries = await query.OrderBy(q => q.CreatedAt).ToListAsync();
        }
    }
}
