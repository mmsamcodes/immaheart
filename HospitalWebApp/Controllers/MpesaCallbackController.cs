using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HospitalWebApp.Data;
using HospitalWebApp.Models;
using HospitalWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalWebApp.Controllers;

[ApiController]
[Route("api/payments")]
public class MpesaCallbackController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PatientEncounterService _encounterService;
    private readonly ILogger<MpesaCallbackController> _logger;
    private readonly string _callbackSecret;

    public MpesaCallbackController(
        ApplicationDbContext context,
        IConfiguration configuration,
        PatientEncounterService encounterService,
        ILogger<MpesaCallbackController> logger)
    {
        _context = context;
        _encounterService = encounterService;
        _logger = logger;
        _callbackSecret = configuration["MpesaSettings:CallbackSecret"] ?? string.Empty;
    }

    [HttpPost("mpesa/callback")]
    public async Task<IActionResult> MpesaCallback()
    {
        if (string.IsNullOrWhiteSpace(_callbackSecret))
        {
            _logger.LogError("M-Pesa callback received without a configured callback secret.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Payment callback is not configured.");
        }

        if (!Request.Headers.TryGetValue("X-Mpesa-Callback-Token", out var suppliedToken) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(suppliedToken.ToString()),
                Encoding.UTF8.GetBytes(_callbackSecret)))
        {
            return Unauthorized("Invalid callback token.");
        }

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(Request.Body);
        }
        catch (JsonException)
        {
            return BadRequest("Invalid JSON payload.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("Body", out var body) ||
                !body.TryGetProperty("stkCallback", out var callback) ||
                !callback.TryGetProperty("ResultCode", out var resultCodeElement) ||
                !resultCodeElement.TryGetInt32(out var resultCode) ||
                !callback.TryGetProperty("CheckoutRequestID", out var checkoutIdElement))
            {
                return BadRequest("Invalid M-Pesa callback format.");
            }

            var checkoutRequestId = checkoutIdElement.GetString();
            if (string.IsNullOrWhiteSpace(checkoutRequestId))
            {
                return BadRequest("M-Pesa callback is missing its checkout reference.");
            }

            var payment = await _context.BillingPayments
                .Include(item => item.Invoice)
                .SingleOrDefaultAsync(item => item.GatewayReference == checkoutRequestId);
            if (payment == null)
            {
                _logger.LogWarning("M-Pesa callback did not match a pending payment for checkout {CheckoutRequestId}.", checkoutRequestId);
                return Ok(new { Result = "Received" });
            }

            if (payment.Status == BillingPayment.Completed)
            {
                return Ok(new { Result = "Already processed" });
            }

            if (payment.Status != BillingPayment.Pending || payment.Invoice == null)
            {
                _logger.LogWarning("M-Pesa callback matched a payment that is not pending: {PaymentId}.", payment.Id);
                return Conflict("Payment is not pending.");
            }
            var invoice = payment.Invoice;

            if (resultCode != 0)
            {
                payment.Status = BillingPayment.Failed;
                payment.Invoice.Status = payment.Invoice.PaidAmount >= payment.Invoice.TotalAmount
                    ? "Paid"
                    : payment.Invoice.PaidAmount > 0 ? "Partial" : "Unpaid";
                await _context.SaveChangesAsync();
                return Ok(new { Result = "Payment not completed" });
            }

            if (!TryReadCallbackMetadata(callback, out var paidAmount, out var receiptNumber))
            {
                return BadRequest("Successful callback is missing payment amount or M-Pesa receipt.");
            }

            if (paidAmount != payment.Amount ||
                invoice.PaidAmount + payment.Amount > invoice.TotalAmount)
            {
                _logger.LogError(
                    "M-Pesa callback amount did not match pending payment {PaymentId}. Expected {ExpectedAmount}, received {ReceivedAmount}.",
                    payment.Id,
                    payment.Amount,
                    paidAmount);
                return Conflict("Callback amount does not match the pending payment.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var claimed = await _context.BillingPayments
                .Where(item => item.Id == payment.Id && item.Status == BillingPayment.Pending)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, BillingPayment.Completed));
            if (claimed != 1)
            {
                await transaction.RollbackAsync();
                return Ok(new { Result = "Already processed" });
            }

            await _context.Entry(invoice).ReloadAsync();
            if (payment.Amount > invoice.TotalAmount - invoice.PaidAmount)
            {
                await transaction.RollbackAsync();
                _logger.LogError("M-Pesa callback would exceed the balance for invoice {InvoiceId}.", payment.InvoiceId);
                return Conflict("Payment would exceed the invoice balance.");
            }

            payment.Status = BillingPayment.Completed;
            payment.ReceiptNumber = receiptNumber;
            payment.Reference = receiptNumber;
            payment.ReceivedAt = DateTime.UtcNow;
            invoice.PaidAmount += payment.Amount;
            payment.BalanceAfter = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
                ? "Paid"
                : "Partial";
            invoice.PaymentConfirmed = invoice.Status == "Paid";
            invoice.PaymentMethod = "MPesa";

            if (invoice.PatientEncounterId.HasValue)
            {
                var encounter = await _context.PatientEncounters
                    .SingleOrDefaultAsync(item => item.Id == invoice.PatientEncounterId.Value);
                if (encounter != null && invoice.Status == "Paid")
                {
                    var pharmacyAwaitingCollection = await _context.PharmacyOrders.AnyAsync(order =>
                        order.BillingInvoiceId == payment.InvoiceId &&
                        order.Status == PharmacyOrder.ReadyForCollection);
                    var otherOutstandingInvoices = await _context.BillingInvoices.AnyAsync(invoice =>
                        invoice.PatientEncounterId == encounter.Id &&
                        invoice.Id != payment.InvoiceId &&
                        invoice.Status != "Paid");

                    if (pharmacyAwaitingCollection)
                    {
                        _encounterService.Transition(
                            encounter,
                            PatientEncounter.AwaitingPharmacy,
                            "Pharmacy",
                            "Your pharmacy bill is settled. Please collect your prepared medicines.",
                            changedById: null);
                    }
                    else if (encounter.Status == PatientEncounter.ReadyToLeave && !otherOutstandingInvoices)
                    {
                        _encounterService.Transition(
                            encounter,
                            PatientEncounter.Completed,
                            "Discharged",
                            "Your visit is complete. Thank you for visiting Immaculate Heart Hospital.",
                            changedById: null,
                            close: true);
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { Result = "Received" });
        }
    }

    private static bool TryReadCallbackMetadata(JsonElement callback, out decimal amount, out string receiptNumber)
    {
        amount = 0;
        receiptNumber = string.Empty;

        if (!callback.TryGetProperty("CallbackMetadata", out var metadata) ||
            !metadata.TryGetProperty("Item", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("Name", out var nameElement) ||
                !item.TryGetProperty("Value", out var valueElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            if (name == "Amount" && valueElement.TryGetDecimal(out var receivedAmount))
            {
                amount = receivedAmount;
            }
            else if (name == "MpesaReceiptNumber")
            {
                receiptNumber = valueElement.GetString() ?? string.Empty;
            }
        }

        return amount > 0 && !string.IsNullOrWhiteSpace(receiptNumber);
    }
}
