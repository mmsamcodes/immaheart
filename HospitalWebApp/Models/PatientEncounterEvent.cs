using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models;

public class PatientEncounterEvent
{
    public int Id { get; set; }

    public int PatientEncounterId { get; set; }

    [ForeignKey(nameof(PatientEncounterId))]
    public PatientEncounter? Encounter { get; set; }

    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(240)]
    public string PatientMessage { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? ChangedById { get; set; }

    [ForeignKey(nameof(ChangedById))]
    public ApplicationUser? ChangedBy { get; set; }
}
