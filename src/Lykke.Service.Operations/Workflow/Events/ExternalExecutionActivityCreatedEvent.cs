using System;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ExternalExecutionActivityCreatedEvent
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Input { get; set; }
    }
}
