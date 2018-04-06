namespace Lykke.Service.Operations.Contracts
{
    public class CreateMarketOrderCommand
    {
        public bool ConfirmationRequired { get; set; }
        public string AssetId { get; set; }
        public AssetPairModel AssetPair { get; set; }
        public double Volume { get; set; }
        public OrderAction OrderAction { get; set; }
        public ClientModel Client { get; set; }        
        public GlobalSettingsModel GlobalSettings { get; set; }        
    }
}
