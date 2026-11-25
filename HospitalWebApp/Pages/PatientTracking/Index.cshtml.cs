using System.Security.Claims;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Pages.PatientTracking;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<PatientEncounter> Encounters { get; private set; } = new List<PatientEncounter>();
    public bool IsPatient { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        var query = _context.PatientEncounters
            .AsNoTracking()
            .Include(encounter => encounter.Patient)
            .Include(encounter => encounter.Events)
            .AsQueryable();

        if (User.IsInRole("Patient"))
        {
            IsPatient = true;
            query = query.Where(encounter => encounter.PatientId == userId);
        }
        else if (User.IsInRole("Admin") || User.IsInRole("Nurse") || User.IsInRole("Reception"))
        {
            query = query.Where(encounter =>
                encounter.ClosedAt == null ||
                encounter.ClosedAt >= DateTime.UtcNow.AddDays(-1));
        }
        else if (User.IsInRole("Doctor"))
        {
            query = query.Where(encounter =>
                encounter.ClosedAt == null &&
                (encounter.AssignedClinicianId == userId ||
                 encounter.AssignedClinicianId == null && encounter.Status == PatientEncounter.AwaitingClinician));
        }
        else if (User.IsInRole("LabTech"))
        {
            query = query.Where(encounter =>
                encounter.ClosedAt == null &&
                encounter.CurrentDepartment == "Diagnostics");
        }
        else if (User.IsInRole("Pharmacy"))
        {
            query = query.Where(encounter =>
                encounter.ClosedAt == null &&
                encounter.CurrentDepartment == "Pharmacy");
        }
        else if (User.IsInRole("Cashier"))
        {
            query = query.Where(encounter =>
                encounter.ClosedAt == null &&
                (encounter.Status == PatientEncounter.ReadyToLeave ||
                 encounter.CurrentDepartment == "Cashier"));
        }
        else
        {
            return Forbid();
        }

        Encounters = await query
            .OrderByDescending(encounter => encounter.LastUpdatedAt)
            .Take(100)
            .ToListAsync();

        return Page();
    }
}
