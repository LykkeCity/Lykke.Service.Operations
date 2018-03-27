namespace Lykke.Service.Operations.Workflow.Data
{
    public class NeededAmountInput
    {
        public OrderAction OrderAction { get; set; }
        public decimal Volume { get; set; }
        public string NeededAssetId { get; set; }
        public string ReceivedAssetId { get; set; }
    }
}
