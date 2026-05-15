using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class HealthRecord
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public string DoctorId { get; set; } = string.Empty;
        [ForeignKey("DoctorId")]
        public virtual ApplicationUser? Doctor { get; set; }

        public int? AppointmentId { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }
        
        public DateTime RecordDate { get; set; } = DateTime.Now;
        public string BloodPressure { get; set; } = string.Empty; // e.g., "120/80"
        public double WeightKg { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Prescription { get; set; } = string.Empty;
        public string DoctorNotes { get; set; } = string.Empty;
    }
}