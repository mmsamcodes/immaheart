using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class DischargeDocument
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public string UploadedById { get; set; } = string.Empty;
        [ForeignKey("UploadedById")]
        public virtual ApplicationUser? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
