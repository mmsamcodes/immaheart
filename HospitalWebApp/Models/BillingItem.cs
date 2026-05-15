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
    }
}