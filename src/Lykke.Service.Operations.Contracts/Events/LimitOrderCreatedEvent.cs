using System;

namespace Lykke.Service.Operations.Contracts.Events
{
    public class LimitOrderCreatedEvent
    {
        public Guid Id { get; set; }
        public OrderAction OrderAction { get; set; }
        public string AssetPairId { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
    }
}
