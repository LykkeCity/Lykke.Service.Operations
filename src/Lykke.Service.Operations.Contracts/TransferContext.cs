using System;
using Lykke.Contracts.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Operations.Contracts
{
    public class TransferContext
    {
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public Guid SourceWalletId { get; set; }
        public Guid WalletId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransferType TransferType { get; set; }
    }
}
