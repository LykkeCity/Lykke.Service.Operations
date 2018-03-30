namespace Lykke.Service.Operations.Contracts
{
    public class CreateLimitOrderCommand
    {
        public string AssetPairId { get; set; }
        public string AssetId { get; set; }
        public double Volume { get; set; }
        public decimal Price { get; set; }
        public AssetShortModel Asset { get; set; }
        public AssetPairModel AssetPair { get; set; }
        public ClientModel Client { get; set; }
        public GlobalSettingsModel GlobalSettings { get; set; }        
    }
}
