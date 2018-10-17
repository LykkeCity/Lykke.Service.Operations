using System;
using MessagePack;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    [ProtoContract]
    public class ConfirmCommand
    {
        [ProtoMember(1)]
        public Guid ClientId { get; set; }

        [ProtoMember(2)]
        public Guid OperationId { get; set; }

        [ProtoMember(3)]
        public string Confirmation { get; set; }
    }
}
