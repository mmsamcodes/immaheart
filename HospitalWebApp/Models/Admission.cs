using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class Admission
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public string AdmittedById { get; set; } = string.Empty;
        [ForeignKey("AdmittedById")]
        public virtual ApplicationUser? AdmittedBy { get; set; }

        public string Ward { get; set; } = string.Empty;
        public string Bed { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; } = DateTime.Now;
        public DateTime? DischargeDate { get; set; }
        public string Status { get; set; } = "Admitted";
        public decimal EstimatedCost { get; set; }
    }
}