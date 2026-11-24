namespace HospitalWebApp.Models;

public class PharmacyOrderItem
{
    public int Id { get; set; }
    public int PharmacyOrderId { get; set; }
    public PharmacyOrder? PharmacyOrder { get; set; }
    public int DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool RequiresPrescription { get; set; }
}
