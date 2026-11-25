using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models;

public class PatientEncounter
{
    public const string AwaitingTriage = "Awaiting triage";
    public const string AwaitingClinician = "Awaiting clinician";
    public const string AwaitingDiagnostics = "Awaiting diagnostics";
    public const string AwaitingPharmacy = "Awaiting pharmacy";
    public const string AwaitingPayment = "Awaiting payment";
    public const string InProgress = "In progress";
    public const string ReadyToLeave = "Ready to leave";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string VisitNumber { get; set; } = $"IH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [ForeignKey(nameof(PatientId))]
    public ApplicationUser? Patient { get; set; }

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = AwaitingTriage;

    [Required]
    [MaxLength(40)]
    public string CurrentDepartment { get; set; } = "Triage";

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Routine";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public string? AssignedClinicianId { get; set; }

    [ForeignKey(nameof(AssignedClinicianId))]
    public ApplicationUser? AssignedClinician { get; set; }

    public ICollection<PatientEncounterEvent> Events { get; set; } = new List<PatientEncounterEvent>();
}
