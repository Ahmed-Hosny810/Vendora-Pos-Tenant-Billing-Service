using Microsoft.Extensions.Options;
using Pos.tenant.Application.Features.SubscriptionPayments.DTOS;
using Pos.tenant.Application.Interfaces.Services;
using Pos.tenant.Infrastructure.Shared.Payment.Paymob;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pos.tenant.Infrastructure.Shared.Services
{
    public class PaymobPaymentService : IPaymobPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly PaymobSettings _settings;

        public PaymobPaymentService(HttpClient httpClient, IOptions<PaymobSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<PaymobCreateIntentionResult> CreateIntentionAsync(PaymobCreateIntentionRequest request, CancellationToken cancellationToken)
        {
            var amountInCents = (int)(request.Amount * 100);
            var integrationId = _settings.IntegrationId;

            var paymobRequest = new
            {
                amount = amountInCents,
                currency = request.Currency,
                payment_methods = new[] { _settings.IntegrationId },

                items = new[]
                {
                    new
                    {
                        name = $"Invoice {request.InvoiceNumber}",
                        amount = amountInCents,
                        description = $"Subscription invoice {request.InvoiceNumber}",
                        quantity = 1
                    }
                },

                billing_data = new
                {
                    apartment = "NA",
                    floor = "NA",
                    first_name = request.CustomerFirstName,
                    last_name = request.CustomerLastName,
                    street = "NA",
                    building = "NA",
                    phone_number = request.CustomerPhoneNumber,
                    shipping_method = "",
                    city = "Cairo",
                    country = "EG",
                    state = "Cairo",
                    email = request.CustomerEmail,
                    postal_code = ""
                },

                extras = new
                {
                    payment_id = request.PaymentId.ToString(),
                    invoice_id = request.InvoiceId.ToString(),
                    invoice_number = request.InvoiceNumber,
                    method = request.Method
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/intention/");

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Token", _settings.SecretKey);

            httpRequest.Content = JsonContent.Create(paymobRequest);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(
               cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Paymob create intention failed. StatusCode: {response.StatusCode}, Body: {responseBody}");
            }

            var paymobResponse = JsonSerializer.Deserialize<PaymobCreateIntentionResponse>(responseBody);

            if (paymobResponse == null)
                throw new InvalidOperationException("Paymob create intention response is empty.");

            if (string.IsNullOrWhiteSpace(paymobResponse.ClientSecret))
                throw new InvalidOperationException("Paymob response does not contain client_secret.");

            if (paymobResponse.IntentionOrderId <= 0)
                throw new InvalidOperationException("Paymob response does not contain a valid intention_order_id.");

            return new PaymobCreateIntentionResult
            {
                ClientSecret = paymobResponse.ClientSecret,
                CheckoutUrl = BuildCheckoutUrl(paymobResponse.ClientSecret),
                ProviderPaymentReference = paymobResponse.IntentionOrderId.ToString(),
                ProviderStatus = paymobResponse.Status ?? "intended"
            };

        }

        public string BuildCheckoutUrl(string clientSecret)
        {
            return $"{_settings.BaseUrl.TrimEnd('/')}/unifiedcheckout/" +
                   $"?publicKey={Uri.EscapeDataString(_settings.PublicKey)}" +
                   $"&clientSecret={Uri.EscapeDataString(clientSecret)}";
        }
    }

    internal class PaymobCreateIntentionResponse
    {
        [JsonPropertyName("payment_keys")]
        public List<PaymobPaymentKeyResponse>? PaymentKeys { get; set; }

        [JsonPropertyName("intention_order_id")]
        public long IntentionOrderId { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("client_secret")]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("confirmed")]
        public bool Confirmed { get; set; }

        [JsonPropertyName("special_reference")]
        public string? SpecialReference { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }
    }

    internal class PaymobPaymentKeyResponse
    {
        [JsonPropertyName("integration")]
        public int Integration { get; set; }

        [JsonPropertyName("gateway_type")]
        public string? GatewayType { get; set; }

        [JsonPropertyName("iframe_id")]
        public int? IframeId { get; set; }

        [JsonPropertyName("order_id")]
        public long OrderId { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }
}
