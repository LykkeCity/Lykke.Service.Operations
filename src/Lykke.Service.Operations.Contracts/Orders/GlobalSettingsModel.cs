namespace Lykke.Service.Operations.Contracts.Orders
{
    /// <summary>
    /// Global settings model
    /// </summary>
    public class GlobalSettingsModel
    {
        public string[] BlockedAssetPairs { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
        public bool BtcOperationsDisabled { get; set; }
        public IcoSettingsModel IcoSettings { get; set; }        
        public FeeSettingsModel FeeSettings { get; set; }
    }
}
