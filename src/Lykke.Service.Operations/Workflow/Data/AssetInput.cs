using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Operations.Contracts.Orders;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class AssetInput
    {
        public string Id { get; set; }
        public string DisplayId { get; set; }
        public bool IsTradable { get; set; }
        public bool IsTrusted { get; set; }
        public OrderAction OrderAction { get; set; }
        public long SiriusAssetId { get; set; }
        public BlockchainIntegrationType BlockchainIntegrationType { get; set; }
    }
}
