using System;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class CompleteActivityCommand
    {
        public Guid OperationId { get; set; }
        public Guid? ActivityId { get; set; }
        public string Output { get; set; }
        public string ActivityType { get; set; }
    }
}
