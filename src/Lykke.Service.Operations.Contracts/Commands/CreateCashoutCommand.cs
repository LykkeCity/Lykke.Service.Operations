using Lykke.Service.Operations.Contracts.Cashout;

namespace Lykke.Service.Operations.Contracts.Commands
{
    public class CreateCashoutCommand
    {
        public string DestinationAddress { get; set; }
        public string DestinationAddressExtension { get; set; }
        public decimal Volume { get; set; }
        public AssetCashoutModel Asset { get; set; }
        public ClientCashoutModel Client { get; set; }
        public GlobalSettingsCashoutModel GlobalSettings { get; set; }
    }
}
