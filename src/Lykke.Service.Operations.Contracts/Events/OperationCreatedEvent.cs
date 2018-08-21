using System;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when operation is created
    /// </summary>
    public class OperationCreatedEvent
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
    }
}
