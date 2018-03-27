namespace Lykke.Service.Operations.Contracts
{
    public class AssetPairModel
    {
        public string Id { get; set; }
        public AssetModel BaseAsset { get; set; }
        public AssetModel QuotingAsset { get; set; }
        public decimal MinVolume { get; set; }
        public decimal MinInvertedVolume { get; set; }
    }
}
