using System;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when operation is failed
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class OperationFailedEvent
    {
        public Guid ClientId { get; set; }
        public Guid OperationId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
