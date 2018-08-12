﻿using MessagePack;

namespace Lykke.Service.Operations.Services
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class SolarCashOutCompletedEvent
    {
        public string ClientId { get; set; }
        public string OperationId { get; set; }
        public string Address { get; set; }
        public decimal Amount { get; set; }
    }
}
