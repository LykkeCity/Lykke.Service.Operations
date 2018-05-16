using Lykke.Service.Assets.Client.Models;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class AssetPairInput
    {
        public string Id { get; set; }
        public string BaseAssetId { get; set; }
        public string BaseAssetDisplayId { get; set; }
        public Blockchain BaseAssetBlockain { get; set; }
        public string QuotingAssetId { get; set; }
        public string QuotingAssetDisplayId { get; set; }
        public Blockchain QuotingAssetBlockchain { get; set; }
        public decimal MinVolume { get; set; }
        public decimal MinInvertedVolume { get; set; }
        public string AssetId { get; set; }
        public decimal Volume { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
        public bool BtcOperationsDisabled { get; set; }
        public string[] BlockedAssetPairs { get; set; }        
    }
}
