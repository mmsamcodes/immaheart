using System.Text.Json;
using HospitalWebApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HospitalWebApp.Controllers;

[ApiController]
[Route("api/payments")]
public class MpesaCallbackController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly string _callbackSecret;

    public MpesaCallbackController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _callbackSecret = configuration["MpesaSettings:CallbackSecret"] ?? string.Empty;
    }

    [HttpPost("mpesa/callback")]
    public async Task<IActionResult> MpesaCallback()
    {
        if (!string.IsNullOrEmpty(_callbackSecret))
        {
            if (!Request.Headers.TryGetValue("X-Mpesa-Callback-Token", out var token) || token != _callbackSecret)
            {
                return Unauthorized("Invalid callback token.");
            }
        }

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest("Empty payload.");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("Body", out var bodyElement) || !bodyElement.TryGetProperty("stkCallback", out var callbackElement))
        {
            return BadRequest("Invalid MPesa callback format.");
        }

        var resultCode = callbackElement.GetProperty("ResultCode").GetInt32();
        var checkoutRequestId = callbackElement.GetProperty("CheckoutRequestID").GetString();
        string accountReference = string.Empty;
        string receiptNumber = string.Empty;
        string phoneNumber = string.Empty;

        if (callbackElement.TryGetProperty("CallbackMetadata", out var callbackMetadata) && callbackMetadata.TryGetProperty("Item", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var itemName = item.GetProperty("Name").GetString();
                if (itemName == "AccountReference")
                {
                    accountReference = item.GetProperty("Value").GetString() ?? string.Empty;
                }
                else if (itemName == "MpesaReceiptNumber")
                {
                    receiptNumber = item.GetProperty("Value").GetString() ?? string.Empty;
                }
                else if (itemName == "PhoneNumber")
                {
                    phoneNumber = item.GetProperty("Value").GetString() ?? string.Empty;
                }
            }
        }

        if (resultCode == 0 && !string.IsNullOrWhiteSpace(accountReference))
        {
            var invoice = _context.BillingInvoices.FirstOrDefault(i => i.InvoiceNumber == accountReference);
            if (invoice != null)
            {
                invoice.Status = "Paid";
                invoice.PaidAmount = invoice.TotalAmount;
                invoice.PaymentConfirmed = true;
                invoice.PaymentMethod = "MPesa";
                invoice.Notes += $"\nMPesa callback received: {receiptNumber} from {phoneNumber} (CheckoutRequestID: {checkoutRequestId}).";
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new { Result = "Received" });
    }
}
