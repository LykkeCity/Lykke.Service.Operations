using System;

namespace Lykke.Service.Operations.Contracts
{
    public class PaymentContext
    {
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public Guid WalletId { get; set; }

    }
}
