using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class CareNote
    {
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        [Required]
        public string AddedById { get; set; } = string.Empty;
        [ForeignKey("AddedById")]
        public virtual ApplicationUser? AddedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string Category { get; set; } = "General"; // General, Observation, Medication, Vital Signs, Progress, Alert
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent
        public bool IsUrgent { get; set; }
    }
}
