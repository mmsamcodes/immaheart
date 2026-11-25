using System.Security.Claims;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Areas.Identity.Pages;

[Authorize(Roles = "Admin,Reception")]
public class ReceptionDashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ReceptionDashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int ActiveVisits { get; private set; }
    public int WaitingForTriage { get; private set; }
    public int AppointmentsToday { get; private set; }
    public IList<PatientEncounter> RecentVisits { get; private set; } = new List<PatientEncounter>();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Reception"))
        {
            return Forbid();
        }

        var today = DateTime.Today;
        ActiveVisits = await _context.PatientEncounters.CountAsync(encounter => encounter.ClosedAt == null);
        WaitingForTriage = await _context.QueueEntries.CountAsync(queue =>
            queue.QueueType == "Triage" && queue.Status == "Waiting");
        AppointmentsToday = await _context.Appointments.CountAsync(appointment =>
            appointment.AppointmentDate >= today &&
            appointment.AppointmentDate < today.AddDays(1) &&
            appointment.Status != "Cancelled");
        RecentVisits = await _context.PatientEncounters
            .AsNoTracking()
            .Include(encounter => encounter.Patient)
            .Where(encounter => encounter.ClosedAt == null)
            .OrderBy(encounter => encounter.StartedAt)
            .Take(12)
            .ToListAsync();

        return Page();
    }
}
