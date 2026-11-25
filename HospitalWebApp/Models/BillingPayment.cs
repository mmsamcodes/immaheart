using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalWebApp.Models;

public class BillingPayment
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Failed = "Failed";

    public int Id { get; set; }
    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public BillingInvoice? Invoice { get; set; }

    [Required]
    [MaxLength(40)]
    public string ReceiptNumber { get; set; } = $"PENDING-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }

    [Required]
    [MaxLength(30)]
    public string Method { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? GatewayReference { get; set; }

    [MaxLength(40)]
    public string? RequestKey { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = Completed;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string? ReceivedById { get; set; }

    [ForeignKey(nameof(ReceivedById))]
    public ApplicationUser? ReceivedBy { get; set; }
}
