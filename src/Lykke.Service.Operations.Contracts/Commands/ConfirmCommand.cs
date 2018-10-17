using System;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ConfirmCommand
    {
        public Guid ClientId { get; set; }

        public Guid OperationId { get; set; }

        public string Confirmation { get; set; }
    }
}
