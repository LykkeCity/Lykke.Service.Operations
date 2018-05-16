using System;

namespace Lykke.Service.Operations.Contracts.Events
{
    /// <summary>
    /// Fired when operation is completed
    /// </summary>
    public class OperationCompletedEvent
    {
        public Guid Id { get; set; }
    }
}
