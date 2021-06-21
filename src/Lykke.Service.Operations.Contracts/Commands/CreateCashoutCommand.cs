using System;
using Lykke.Service.Operations.Contracts.Cashout;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CreateCashoutCommand
    {
        public Guid OperationId { get; set; }
        public string WalletId { get; set; }
        public string DestinationAddress { get; set; }
        public string DestinationAddressExtension { get; set; }
        public decimal Volume { get; set; }
        public AssetCashoutModel Asset { get; set; }
        public ClientCashoutModel Client { get; set; }
        public GlobalSettingsCashoutModel GlobalSettings { get; set; }
    }
}
