using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Pos.tenant.Infrastructure.Shared.Payment.Paymob
{
    internal class PaymobCreateIntentionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("intention_order_id")]
        public long IntentionOrderId { get; set; }

        [JsonPropertyName("client_secret")]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("confirmed")]
        public bool Confirmed { get; set; }
    }
}
