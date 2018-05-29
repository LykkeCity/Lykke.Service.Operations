using Lykke.Service.Operations.Contracts.Orders;

namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Command to create limit order
    /// </summary>
    public class CreateLimitOrderCommand
    {
        public AssetPairModel AssetPair { get; set; }
        public double Volume { get; set; }
        public decimal Price { get; set; }
        public OrderAction OrderAction { get; set; }
        public ClientModel Client { get; set; }
        public GlobalSettingsModel GlobalSettings { get; set; }
    }
}
