namespace Lykke.Service.Operations.Contracts.Cashout
{
    public class AssetCashoutModel
    {
        public string Id { get; set; }
        public string DisplayId { get; set; }
        public int MultiplierPower { get; set; }
        public string AssetAddress { get; set; }
        public int Accuracy { get; set; }
        public string Blockchain { get; set; }
        public string Type { get; set; }
        public bool IsTradable { get; set; }
        public bool IsTrusted { get; set; }
        public bool KycNeeded { get; set; }
        public string BlockchainIntegrationLayerId { get; set; }
        public decimal CashoutMinimalAmount { get; set; }
        public decimal LowVolumeAmount { get; set; }
    }
}
