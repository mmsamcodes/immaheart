using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class CarePlanEntry
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public int? TriageEntryId { get; set; }
        [ForeignKey("TriageEntryId")]
        public virtual TriageEntry? TriageEntry { get; set; }

        public string CreatedById { get; set; } = string.Empty;
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Instructions { get; set; } = string.Empty;
        public string Goals { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime? ReviewDate { get; set; }
    }
}
