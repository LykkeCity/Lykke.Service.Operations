namespace Lykke.Service.Operations.Contracts
{
    public class CreateOrderCommand
    {
        public string AssetPairId { get; set; }
        public string AssetId { get; set; }
        public decimal Volume { get; set; }                
        public ClientModel Client { get; set; }        
        public GlobalSettingsModel GlobalSettings { get; set; }        
    }
}
