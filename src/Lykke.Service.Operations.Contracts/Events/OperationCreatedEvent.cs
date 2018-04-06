using System;

namespace Lykke.Service.Operations.Contracts.Events
{
    public class OperationCreatedEvent
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public bool ConfirmationRequired { get; set; }
    }
}
