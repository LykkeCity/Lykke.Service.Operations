using System;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ExternalExecutionActivityCreatedEvent
    {
        public Guid OperationId { get; set; }
        public Guid ActivityId { get; set; }
        public string Type { get; set; }
        public string Input { get; set; }
    }
}
