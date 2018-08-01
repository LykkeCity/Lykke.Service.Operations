    namespace Lykke.Service.Operations.Workflow
{
    public class EthInput
    {
        public string AssetId { get; set; }
        public decimal Volume { get; set; }
        public string DestinationAddress { get; set; }
        public string HotwalletAddress { get; set; }
        public decimal AdapterBalance { get; set; }
        public bool CashoutIsAllowed { get; set; }
    }
}
