using System;
using Lykke.Contracts.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Operations.Core.Domain
{
    public class PaymentContext
    {        
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PaymentType PaymentType { get; set; }
    }
}
