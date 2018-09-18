using System;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when operation confirmation is necessary
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class OperationConfirmationRequestedEvent
    {
        public Guid ClientId { get; set; }

        public Guid OperationId { get; set; }

        public string ConfirmationType { get; set; }
    }
}
