using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class QueueEntry
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public string AssignedById { get; set; } = string.Empty;
        [ForeignKey("AssignedById")]
        public virtual ApplicationUser? AssignedBy { get; set; }

        public string QueueType { get; set; } = "Doctor";
        public string Status { get; set; } = "Waiting";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }
        public string Notes { get; set; } = string.Empty;

        public string? AssignedDoctorId { get; set; }
        [ForeignKey("AssignedDoctorId")]
        public virtual ApplicationUser? AssignedDoctor { get; set; }

        public string Room { get; set; } = "General";
    }
}
