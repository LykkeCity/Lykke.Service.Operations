using System;

namespace Lykke.Service.Operations.Workflow.Commands
{
    public class CompleteActivityCommand
    {
        public Guid OperationId { get; set; }
        public Guid? ActivityId { get; set; }
        public string Output { get; set; }        
    }
}
