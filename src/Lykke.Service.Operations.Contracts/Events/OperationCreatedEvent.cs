using System;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when operation is created
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class OperationCreatedEvent
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public OperationType OperationType { get; set; }
    }
}
