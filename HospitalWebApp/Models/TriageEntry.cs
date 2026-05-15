using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class TriageEntry
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientIdentifier { get; set; } = string.Empty;

        public string NurseId { get; set; } = string.Empty;
        [ForeignKey("NurseId")]
        public virtual ApplicationUser? Nurse { get; set; }

        public DateTime TriageDate { get; set; } = DateTime.Now;
        public string BloodPressure { get; set; } = string.Empty;
        public double TemperatureC { get; set; }
        public int HeartRate { get; set; }
        public int RespiratoryRate { get; set; }
        public int OxygenSaturation { get; set; }
        public double WeightKg { get; set; }
        public double HeightCm { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string PrimaryConcern { get; set; } = string.Empty;
        public string Status { get; set; } = "PendingDoctor";
        public string NextStep { get; set; } = "Doctor";
        public string? AssignedDoctorId { get; set; }
        [ForeignKey("AssignedDoctorId")]
        public virtual ApplicationUser? AssignedDoctor { get; set; }
    }
}
