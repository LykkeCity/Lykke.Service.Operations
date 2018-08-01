namespace Lykke.Service.Operations.Workflow
{
    public class EthCashoutEstimationInput
    {
        public string OperationId { get; set; }
        public string AssetAddress { get; set; }
        public int AssetMultiplierPower { get; set; }
        public int AssetAccuracy { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Volume { get; set; }
    }
}
