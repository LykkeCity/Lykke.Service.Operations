using Lykke.MatchingEngine.Connector.Models.Api;
using OrderAction = Lykke.Service.Operations.Contracts.Orders.OrderAction;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class MeMoOrderInput
    {
        public string Id { get; set; }        
        public string AssetPairId { get; set; }
        public string ClientId { get; set; }
        public bool Straight { get; set; }
        public double Volume { get; set; }        
        public OrderAction OrderAction { get; set; }
        public MarketOrderFeeModel Fee { get; set; }        
    }

    public class MeLoOrderInput
    {
        public string Id { get; set; }        
        public string AssetPairId { get; set; }
        public string ClientId { get; set; }
        public OrderAction OrderAction { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }        
        public LimitOrderFeeModel Fee { get; set; }        
    }
}
