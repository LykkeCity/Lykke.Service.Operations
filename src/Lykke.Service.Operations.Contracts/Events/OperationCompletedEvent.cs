using System;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when operation is confirmed
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class OperationCompletedEvent
    {
        public Guid ClientId { get; set; }
        public Guid OperationId { get; set; }
    }
}
