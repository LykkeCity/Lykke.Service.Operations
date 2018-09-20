using System;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when operation is corrupted
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class OperationCorruptedEvent
    {
        public Guid ClientId { get; set; }
        public Guid OperationId { get; set; }
    }
}
