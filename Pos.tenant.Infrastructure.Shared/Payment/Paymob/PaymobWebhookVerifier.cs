using Microsoft.Extensions.Options;
using Pos.tenant.Application.DTOS;
using Pos.tenant.Application.Interfaces.Payment;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Pos.tenant.Infrastructure.Shared.Payment.Paymob
{
    public class PaymobWebhookVerifier : IPaymobWebhookVerifier
    {
        private readonly PaymobSettings _settings;

        public PaymobWebhookVerifier(IOptions<PaymobSettings> options)
        {
            _settings = options.Value;
        }
        public PaymobWebhookResult VerifyAndParse(PaymobWebhookRequest request, string? hmac)
        {
            if (request.Obj == null)
                throw new InvalidOperationException("Invalid Paymob callback payload.");

            if (string.IsNullOrWhiteSpace(hmac))
                throw new InvalidOperationException("Missing Paymob HMAC.");

            var calculatedHmac = CalculateHmac(request);

            if (!FixedTimeEquals(calculatedHmac, hmac))
                throw new InvalidOperationException("Invalid Paymob HMAC.");

            if (request.Obj.IntegrationId != _settings.IntegrationId)
                throw new InvalidOperationException("Invalid Paymob integration id.");

            return ParseWebhook(request);
        }
        private string CalculateHmac(PaymobWebhookRequest request)
        {
            var obj = request.Obj;
            var sourceData = obj.SourceData ?? new PaymobSourceData();

            var values = new[]
            {
                ToStringValue(obj.AmountCents),
                obj.CreatedAt ?? string.Empty,
                obj.Currency ?? string.Empty,
                ToStringValue(obj.ErrorOccured),
                ToStringValue(obj.HasParentTransaction),
                ToStringValue(obj.Id),
                ToStringValue(obj.IntegrationId),
                ToStringValue(obj.Is3DSecure),
                ToStringValue(obj.IsAuth),
                ToStringValue(obj.IsCapture),
                ToStringValue(obj.IsRefunded),
                ToStringValue(obj.IsStandalonePayment),
                ToStringValue(obj.IsVoided),
                ToStringValue(obj.Order?.Id ?? 0),
                ToStringValue(obj.Owner),
                ToStringValue(obj.Pending),
                sourceData.Pan ?? string.Empty,
                sourceData.SubType ?? string.Empty,
                sourceData.Type ?? string.Empty,
                ToStringValue(obj.Success)
            };

            var concatenated = string.Concat(values);

            using var hmacSha512 = new HMACSHA512(
                Encoding.UTF8.GetBytes(_settings.HmacSecret));

            var hashBytes = hmacSha512.ComputeHash(
                Encoding.UTF8.GetBytes(concatenated));

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private PaymobWebhookResult ParseWebhook(PaymobWebhookRequest request)
        {
            var obj = request.Obj;

            var success = obj.Success;
            var pending = obj.Pending;

            return new PaymobWebhookResult
            {
                PaymentId = TryGetInternalPaymentId(request),
                ProviderPaymentReference = obj.Order?.Id.ToString(CultureInfo.InvariantCulture),
                ProviderTransactionId = obj.Id.ToString(CultureInfo.InvariantCulture),
                ProviderStatus = BuildProviderStatus(success, pending, obj.ErrorOccured),
                Success = success,
                Pending = pending,
                AmountCents = obj.AmountCents,
                Currency = obj.Currency,
                FailureReason = success
                    ? null
                    : obj.Data?.Message ?? "Paymob transaction failed."
            };
        }

        private static string BuildProviderStatus(
            bool success,
            bool pending,
            bool errorOccured)
        {
            if (pending)
                return "pending";

            if (success && !errorOccured)
                return "success";

            return "failed";
        }

        private static Guid? TryGetInternalPaymentId(PaymobWebhookRequest request)
        {
            var paymentId =
                TryReadString(request.Obj.PaymentKeyClaims?.Extra, "payment_id") ??
                TryReadString(request.Obj.Extras, "payment_id") ??
                TryReadString(request.Obj.Order?.Extras, "payment_id") ??
                TryReadNestedString(request.Obj.PaymentKeyClaims?.Extra, "creation_extras", "payment_id") ??
                TryReadNestedString(request.Obj.Extras, "creation_extras", "payment_id") ??
                TryReadNestedString(request.Obj.Order?.Extras, "creation_extras", "payment_id");

            if (Guid.TryParse(paymentId, out var value))
                return value;

            return null;
        }

        private static string? TryReadString(JsonElement? jsonElement, string propertyName)
        {
            if (!jsonElement.HasValue)
                return null;

            var element = jsonElement.Value;

            if (element.ValueKind != JsonValueKind.Object)
                return null;

            if (!element.TryGetProperty(propertyName, out var property))
                return null;

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static string? TryReadNestedString(
            JsonElement? jsonElement,
            string parentPropertyName,
            string childPropertyName)
        {
            if (!jsonElement.HasValue)
                return null;

            var element = jsonElement.Value;

            if (element.ValueKind != JsonValueKind.Object)
                return null;

            if (!element.TryGetProperty(parentPropertyName, out var parent))
                return null;

            if (parent.ValueKind != JsonValueKind.Object)
                return null;

            if (!parent.TryGetProperty(childPropertyName, out var child))
                return null;

            return child.ValueKind switch
            {
                JsonValueKind.String => child.GetString(),
                JsonValueKind.Number => child.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static string ToStringValue(bool value)
        {
            return value ? "true" : "false";
        }

        private static string ToStringValue(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool FixedTimeEquals(string calculatedHmac, string receivedHmac)
        {
            var calculatedBytes = Encoding.UTF8.GetBytes(
                calculatedHmac.Trim().ToLowerInvariant());

            var receivedBytes = Encoding.UTF8.GetBytes(
                receivedHmac.Trim().ToLowerInvariant());

            return calculatedBytes.Length == receivedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(
                       calculatedBytes,
                       receivedBytes);
        }
    }
}
