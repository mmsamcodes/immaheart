using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class PatientService
    {
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        [Required]
        public int ServiceCatalogId { get; set; }
        [ForeignKey("ServiceCatalogId")]
        public virtual ServiceCatalog? Service { get; set; }

        [Required]
        public string AssignedById { get; set; } = string.Empty;
        [ForeignKey("AssignedById")]
        public virtual ApplicationUser? AssignedBy { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Cancelled
        public string Notes { get; set; } = string.Empty;
        public DateTime? ScheduledDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
