using Lykke.Service.Operations.Contracts.Orders;

namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Command to create stop limit order
    /// </summary>
    public class CreateStopLimitOrderCommand
    {
        public AssetPairModel AssetPair { get; set; }
        public double Volume { get; set; }
        public decimal? LowerLimitPrice { get; set; }
        public decimal? LowerPrice { get; set; }
        public decimal? UpperLimitPrice { get; set; }
        public decimal? UpperPrice { get; set; }
        public OrderAction OrderAction { get; set; }
        public ClientModel Client { get; set; }
        public GlobalSettingsModel GlobalSettings { get; set; }
    }
}
