using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.Video
{
    [Authorize]
    public class SessionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SessionsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Appointment> VideoSessions { get; set; } = new List<Appointment>();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Admin") || User.IsInRole("Accounts"))
            {
                VideoSessions = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.IsVirtual || a.VideoSessionId != null)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
            }
            else if (User.IsInRole("Doctor") && !string.IsNullOrEmpty(userId))
            {
                VideoSessions = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => (a.IsVirtual || a.VideoSessionId != null) && a.DoctorId == userId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
            }
            else if (!string.IsNullOrEmpty(userId))
            {
                VideoSessions = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => (a.IsVirtual || a.VideoSessionId != null) && a.PatientId == userId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
            }
            else
            {
                VideoSessions = new List<Appointment>();
            }
        }
    }
}
