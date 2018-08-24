using System;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class FailActivityCommand
    {
        public Guid OperationId { get; set; }
        public Guid? ActivityId { get; set; }
        public string Output { get; set; }        
    }
}
