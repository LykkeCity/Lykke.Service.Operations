using System;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Events
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class MeCashoutFailedEvent
    {
        public Guid OperationId { get; set; }
        public Guid RequestId { get; set; }

        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }        
    }
}
