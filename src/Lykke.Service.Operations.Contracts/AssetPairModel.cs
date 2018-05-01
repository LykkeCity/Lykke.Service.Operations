namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Asset pair model
    /// </summary>
    public class AssetPairModel
    {
        public string Id { get; set; }
        public AssetModel BaseAsset { get; set; }
        public AssetModel QuotingAsset { get; set; }
        public double MinVolume { get; set; }
        public double MinInvertedVolume { get; set; }
    }
}
