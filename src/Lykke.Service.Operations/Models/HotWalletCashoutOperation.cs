namespace Lykke.Service.Operations.Models
{
    public class HotWalletCashoutOperation
    {
        public string ClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Volume { get; set; }
        public string DestinationAddress { get; set; }
    }
}
