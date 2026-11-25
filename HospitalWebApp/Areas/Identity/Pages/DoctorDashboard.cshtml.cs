using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using HospitalWebApp.Models;
using Microsoft.EntityFrameworkCore;
using HospitalWebApp.Data;

namespace HospitalWebApp.Areas.Identity.Pages
{
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DoctorDashboardModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string FullName { get; set; } = "";
        public int TodayAppointmentsCount { get; set; }
        public int TotalPatientsCount { get; set; }
        public int PendingAppointmentsCount { get; set; }
        public int QueuedPatientsCount { get; set; }
        public int VirtualAppointmentsCount { get; set; }
        public int InPersonAppointmentsCount { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; } = new();
        public List<ApplicationUser> RecentPatients { get; set; } = new();
        public List<QueueEntry> DoctorQueueEntries { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Check if user is an admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToPage("/AdminDashboard");
            }
            // Check if user is a doctor
            if (!await _userManager.IsInRoleAsync(user, "Doctor"))
            {
                return RedirectToPage("/Dashboard");
            }

            FullName = user.FullName;

            var today = DateTime.Today;
            var doctorId = user.Id;

            // Clinical work is not gated by payment confirmation.
            TodayAppointmentsCount = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == today && a.Status != "Cancelled")
                .CountAsync();

            // Total patients assigned to the clinician.
            TotalPatientsCount = await _context.Appointments
                .Where(a => a.DoctorId == doctorId)
                .Select(a => a.PatientId)
                .Distinct()
                .CountAsync();

            // Pending appointments (awaiting doctor confirmation or action)
            PendingAppointmentsCount = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Status == "Pending")
                .CountAsync();

            DoctorQueueEntries = await _context.QueueEntries
                .Include(q => q.Patient)
                .Include(q => q.AssignedBy)
                .Where(q => q.QueueType == "Doctor" &&
                            q.Status == "Waiting" &&
                            (q.AssignedDoctorId == null || q.AssignedDoctorId == doctorId))
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();

            QueuedPatientsCount = DoctorQueueEntries.Count;
            VirtualAppointmentsCount = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.IsVirtual && a.Status != "Cancelled")
                .CountAsync();

            InPersonAppointmentsCount = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && !a.IsVirtual && a.Status != "Cancelled")
                .CountAsync();

            // Upcoming clinical appointments (next 7 days).
            var nextWeek = today.AddDays(7);
            UpcomingAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                            a.AppointmentDate >= today &&
                            a.AppointmentDate <= nextWeek &&
                            a.Status != "Cancelled")
                .Include(a => a.Patient)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            // Recent patients assigned to the clinician.
            var recentDate = today.AddDays(-30);
            var recentPatientIds = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= recentDate)
                .Select(a => a.PatientId)
                .Distinct()
                .Take(10)
                .ToListAsync();

            RecentPatients = await _context.Users
                .Where(u => recentPatientIds.Contains(u.Id))
                .ToListAsync();

            return Page();
        }
    }
}