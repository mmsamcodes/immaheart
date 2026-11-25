using HospitalWebApp.Data;
using HospitalWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Services;

public class PatientEncounterService
{
    private readonly ApplicationDbContext _context;

    public PatientEncounterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<PatientEncounter?> FindActiveAsync(string patientId)
    {
        return _context.PatientEncounters
            .Where(encounter =>
                encounter.PatientId == patientId &&
                encounter.ClosedAt == null &&
                encounter.Status != PatientEncounter.Cancelled)
            .OrderByDescending(encounter => encounter.StartedAt)
            .FirstOrDefaultAsync();
    }

    public PatientEncounter Start(
        string patientId,
        string? changedById,
        string status,
        string department,
        string patientMessage,
        string priority = "Routine")
    {
        var encounter = new PatientEncounter
        {
            PatientId = patientId,
            Status = status,
            CurrentDepartment = department,
            Priority = priority,
            StartedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _context.PatientEncounters.Add(encounter);
        AddEvent(encounter, status, department, patientMessage, changedById);
        return encounter;
    }

    public void Transition(
        PatientEncounter encounter,
        string status,
        string department,
        string patientMessage,
        string? changedById,
        bool close = false)
    {
        encounter.Status = status;
        encounter.CurrentDepartment = department;
        encounter.LastUpdatedAt = DateTime.UtcNow;
        encounter.ClosedAt = close ? DateTime.UtcNow : null;
        AddEvent(encounter, status, department, patientMessage, changedById);
    }

    private void AddEvent(
        PatientEncounter encounter,
        string status,
        string department,
        string patientMessage,
        string? changedById)
    {
        _context.PatientEncounterEvents.Add(new PatientEncounterEvent
        {
            Encounter = encounter,
            Status = status,
            Department = department,
            PatientMessage = patientMessage,
            OccurredAt = DateTime.UtcNow,
            ChangedById = changedById
        });
    }
}
