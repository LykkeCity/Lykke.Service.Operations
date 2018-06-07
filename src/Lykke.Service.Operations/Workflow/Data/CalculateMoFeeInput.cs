using Lykke.Service.Operations.Contracts.Orders;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class CalculateMoFeeInput
    {        
        public string ClientId { get; set; }        
        public string AssetPairId { get; set; }        
        public string AssetId { get; set; }
        public OrderAction OrderAction { get; set; }
        public string TargetClientId { get; set; }        
    }

    public class CalculateLoFeeInput
    {
        public string ClientId { get; set; }
        public string AssetPairId { get; set; }
        public string BaseAssetId { get; set; }        
        public OrderAction OrderAction { get; set; }
        public string TargetClientId { get; set; }
    }
}
