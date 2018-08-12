    using System;
    using MessagePack;

namespace Lykke.Service.Operations.Services
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class FailActivityCommand
    {
        public Guid OperationId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
