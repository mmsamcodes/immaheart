using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HospitalWebApp.Data;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace HospitalWebApp.Services;

public interface IPaymentService
{
    Task<string> CreateMpesaStkPushAsync(string phoneNumber, decimal amount, string accountReference, string transactionDesc);
    Task<string> CreateStripeCheckoutSessionAsync(decimal amount, string invoiceNumber, string successUrl, string cancelUrl);
    bool IsMpesaConfigured { get; }
    bool IsStripeConfigured { get; }
}

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MpesaSettings _mpesa;
    private readonly StripeSettings _stripe;

    public bool IsMpesaConfigured { get; }
    public bool IsStripeConfigured { get; }

    public PaymentService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _mpesa = config.GetSection("MpesaSettings").Get<MpesaSettings>() ?? new MpesaSettings();
        _stripe = config.GetSection("StripeSettings").Get<StripeSettings>() ?? new StripeSettings();

        IsMpesaConfigured = !string.IsNullOrWhiteSpace(_mpesa.ConsumerKey)
            && !string.IsNullOrWhiteSpace(_mpesa.ConsumerSecret)
            && !string.IsNullOrWhiteSpace(_mpesa.ShortCode)
            && !string.IsNullOrWhiteSpace(_mpesa.Passkey)
            && !string.IsNullOrWhiteSpace(_mpesa.CallbackBaseUrl);

        IsStripeConfigured = !string.IsNullOrWhiteSpace(_stripe.SecretKey)
            && !string.IsNullOrWhiteSpace(_stripe.PublishableKey);
    }

    public async Task<string> CreateMpesaStkPushAsync(string phoneNumber, decimal amount, string accountReference, string transactionDesc)
    {
        if (!IsMpesaConfigured)
        {
            throw new InvalidOperationException("MPesa is not configured. Set the MPesa settings in configuration.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        var accessToken = await GetMpesaAccessTokenAsync();
        var formattedPhone = NormalizePhoneNumber(phoneNumber);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_mpesa.ShortCode}{_mpesa.Passkey}{timestamp}"));
        var callbackUrl = _mpesa.CallbackBaseUrl.TrimEnd('/') + "/api/payments/mpesa/callback";

        var requestBody = new
        {
            BusinessShortCode = _mpesa.ShortCode,
            Password = password,
            Timestamp = timestamp,
            TransactionType = "CustomerPayBillOnline",
            Amount = amount,
            PartyA = formattedPhone,
            PartyB = _mpesa.ShortCode,
            PhoneNumber = formattedPhone,
            CallBackURL = callbackUrl,
            AccountReference = accountReference,
            TransactionDesc = transactionDesc
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.PostAsync(GetMpesaStkPushUrl(), new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"MPesa STK push failed: {response.StatusCode} - {body}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.TryGetProperty("CheckoutRequestID", out var checkoutIdProperty))
        {
            return checkoutIdProperty.GetString() ?? string.Empty;
        }

        throw new InvalidOperationException("MPesa STK push returned no CheckoutRequestID.");
    }

    public async Task<string> CreateStripeCheckoutSessionAsync(decimal amount, string invoiceNumber, string successUrl, string cancelUrl)
    {
        if (!IsStripeConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured. Set the Stripe settings in configuration.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        StripeConfiguration.ApiKey = _stripe.SecretKey;

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = amount,
                        Currency = _stripe.Currency ?? "kes",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Invoice {invoiceNumber}",
                            Description = "Hospital services and consultation"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url ?? throw new InvalidOperationException("Stripe checkout session did not return a URL.");
    }

    private async Task<string> GetMpesaAccessTokenAsync()
    {
        var client = _httpClientFactory.CreateClient();
        var tokenUrl = _mpesa.UseSandbox ? "https://sandbox.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials" : "https://api.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_mpesa.ConsumerKey}:{_mpesa.ConsumerSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await client.GetAsync(tokenUrl);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Unable to fetch MPesa access token: {response.StatusCode} - {content}");
        }

        using var document = JsonDocument.Parse(content);
        if (document.RootElement.TryGetProperty("access_token", out var tokenProperty))
        {
            return tokenProperty.GetString() ?? string.Empty;
        }

        throw new InvalidOperationException("MPesa access token response did not contain an access_token.");
    }

    private static string NormalizePhoneNumber(string phone)
    {
        var trimmed = phone?.Trim() ?? string.Empty;
        if (trimmed.StartsWith("+")) trimmed = trimmed.Substring(1);
        if (trimmed.StartsWith("0") && trimmed.Length == 10)
        {
            return "254" + trimmed.Substring(1);
        }
        return trimmed;
    }

    private string GetMpesaStkPushUrl() => _mpesa.UseSandbox ? "https://sandbox.safaricom.co.ke/mpesa/stkpush/v1/processrequest" : "https://api.safaricom.co.ke/mpesa/stkpush/v1/processrequest";

    private class MpesaSettings
    {
        public string ConsumerKey { get; set; } = string.Empty;
        public string ConsumerSecret { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public string Passkey { get; set; } = string.Empty;
        public string CallbackBaseUrl { get; set; } = string.Empty;
        public string CallbackSecret { get; set; } = string.Empty;
        public bool UseSandbox { get; set; } = true;
    }

    private class StripeSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string Currency { get; set; } = "kes";
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
