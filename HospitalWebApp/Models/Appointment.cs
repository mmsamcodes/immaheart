using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;

        // Navigation property for the patient linked to this appointment.
        [ForeignKey("PatientId")]
        public virtual ApplicationUser? Patient { get; set; }

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        // Navigation property for the doctor assigned to this appointment.
        [ForeignKey("DoctorId")]
        public virtual ApplicationUser? Doctor { get; set; }

        [Required]
        [Display(Name = "Appointment Date & Time")]
        public DateTime AppointmentDate { get; set; }

        [Display(Name = "Symptoms Summary")]
        public string SymptomsSummary { get; set; } = string.Empty;
        
        public string Status { get; set; } = "Pending"; 
        
        public bool IsVirtual { get; set; } = false;

        // Optional invoice linked to this appointment.
        public int? InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public virtual BillingInvoice? Invoice { get; set; }

        // Fields that track whether payment has been received.
        public bool PaymentConfirmed { get; set; } = false;
        public string PaymentMethod { get; set; } = string.Empty; // e.g. MPesa, Visa
        public string PaymentReference { get; set; } = string.Empty; // Transaction identifier

        // Virtual appointment session details.
        public string? VideoSessionId { get; set; }
        public string? VideoJoinUrl { get; set; }
        public bool VirtualConfirmedByDoctor { get; set; } = false;
    }
}