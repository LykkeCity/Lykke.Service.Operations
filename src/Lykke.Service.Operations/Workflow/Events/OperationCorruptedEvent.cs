using System;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class OperationCorruptedEvent
    {
        public Guid OperationId { get; set; }
    }
}
