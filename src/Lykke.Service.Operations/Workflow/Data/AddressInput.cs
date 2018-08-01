
namespace Lykke.Service.Operations.Workflow.Data
{
    public class AddressInput
    {
        public string AssetId { get; set; }
        public string AssetBlockchain { get; set; } 
        public string DestinationAddress { get; set; }
        public string BlockchainIntegrationLayerId { get; set; }
        public string Multisig { get; set; }
        public string AssetType { get; set; }
    }
}
