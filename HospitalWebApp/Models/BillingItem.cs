using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models
{
    public class BillingItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public virtual BillingInvoice Invoice { get; set; } = null!;

        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public int? PatientServiceId { get; set; }
        [ForeignKey(nameof(PatientServiceId))]
        public virtual PatientService? PatientService { get; set; }

        public int? LabOrderId { get; set; }
        [ForeignKey(nameof(LabOrderId))]
        public virtual LabOrder? LabOrder { get; set; }
    }
}