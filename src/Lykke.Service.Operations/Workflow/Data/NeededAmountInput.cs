using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class NeededAmountInput
    {
        public OperationType OperationType { get; set; }
        public OrderAction OrderAction { get; set; }
        public decimal Volume { get; set; }
        public decimal? Price { get; set; }
        public string NeededAssetId { get; set; }
        public string ReceivedAssetId { get; set; }
        public string AssetId { get; set; }
        public string BaseAssetId { get; set; }
    }
}
