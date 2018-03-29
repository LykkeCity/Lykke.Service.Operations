using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class CalculateFeeInput
    {        
        public string ClientId { get; set; }
        public OperationType OperationType { get; set; }
        public string AssetPairId { get; set; }
        public string BaseAssetId { get; set; }
        public string AssetId { get; set; }
        public OrderAction OrderAction { get; set; }
        public string TargetClientId { get; set; }        
    }
}
