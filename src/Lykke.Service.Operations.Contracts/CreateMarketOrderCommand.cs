using Lykke.Service.Operations.Contracts.Orders;

namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Command to create market order
    /// </summary>
    public class CreateMarketOrderCommand
    {
        public string AssetId { get; set; }
        public AssetPairModel AssetPair { get; set; }
        public double Volume { get; set; }
        public OrderAction OrderAction { get; set; }
        public ClientModel Client { get; set; }        
        public GlobalSettingsModel GlobalSettings { get; set; }        
    }
}
