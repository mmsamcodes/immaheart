using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class LabOrder
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public string DoctorId { get; set; } = string.Empty;
        [ForeignKey("DoctorId")]
        public virtual ApplicationUser? Doctor { get; set; }

        public string TestName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal Cost { get; set; }

        public DateTime OrderedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Ordered";
        public DateTime? CollectedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ResultSummary { get; set; } = string.Empty;
        public string? ResultFileName { get; set; }
    }
}