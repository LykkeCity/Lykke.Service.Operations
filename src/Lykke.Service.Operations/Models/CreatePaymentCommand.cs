using System;
using Lykke.Contracts.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Operations.Models
{
    public class CreatePaymentCommand
    {
        public Guid ClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }        
        public PaymentType PaymentType { get; set; }
    }
}
