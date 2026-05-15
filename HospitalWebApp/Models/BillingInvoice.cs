using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class BillingInvoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
        public string PatientId { get; set; } = string.Empty;
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = "Unpaid";
        public bool PaymentConfirmed { get; set; } = false;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public virtual ICollection<BillingItem> Items { get; set; } = new List<BillingItem>();
        
        // Optional link to an appointment when invoice is created for a consultation
        public int? AppointmentId { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        // Insurance details (optional)
        public string InsuranceScheme { get; set; } = string.Empty;
        public string InsuranceNumber { get; set; } = string.Empty;
        public decimal InsuranceCoveragePercent { get; set; } = 0m; // e.g., 80 for 80%
    }
}