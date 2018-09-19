using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ConfirmationReceivedEvent
    {
        public Guid OperationId { get; set; }

        public string Confirmation { get; set; }
    }
}
