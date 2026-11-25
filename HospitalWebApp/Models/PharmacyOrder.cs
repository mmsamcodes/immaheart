using System.ComponentModel.DataAnnotations;

namespace HospitalWebApp.Models;

public class PharmacyOrder
{
    public const string PendingReview = "Pending review";
    public const string Approved = "Approved";
    public const string ReadyForCollection = "Ready for collection";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";

    public int Id { get; set; }
    [Required]
    public string OrderNumber { get; set; } = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    [Required]
    public string PatientId { get; set; } = string.Empty;
    public ApplicationUser? Patient { get; set; }
    public int? PatientEncounterId { get; set; }
    public PatientEncounter? PatientEncounter { get; set; }
    public int? BillingInvoiceId { get; set; }
    public BillingInvoice? BillingInvoice { get; set; }
    public string? PrescriberId { get; set; }
    public ApplicationUser? Prescriber { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public string Status { get; set; } = PendingReview;
    [MaxLength(500)]
    public string PatientNote { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string? PrescriptionText { get; set; }
    public byte[]? PrescriptionDocument { get; set; }
    public string? PrescriptionContentType { get; set; }
    public ICollection<PharmacyOrderItem> Items { get; set; } = new List<PharmacyOrderItem>();
}
