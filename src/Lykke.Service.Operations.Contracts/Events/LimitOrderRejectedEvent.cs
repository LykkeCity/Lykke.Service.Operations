using System;
using Lykke.Service.Operations.Contracts.Orders;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when limit order is rejected
    /// </summary>
    public class LimitOrderRejectedEvent
    {
        public Guid Id { get; set; }
        public OrderAction OrderAction { get; set; }
        public string AssetPairId { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
    }
}
