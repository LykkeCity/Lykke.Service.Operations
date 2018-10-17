using System;

namespace Lykke.Service.Operations.Contracts.Commands
{
    public class CreateTransferCommand
    {
        public Guid ClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public Guid SourceWalletId { get; set; }
        public Guid WalletId { get; set; }        
    }
}
