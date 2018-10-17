using System;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Commands
{
    [ProtoContract]
    public class CreateTransferCommand
    {
        [ProtoMember(1)]
        public Guid ClientId { get; set; }
        [ProtoMember(2)]
        public string AssetId { get; set; }
        [ProtoMember(3)]
        public decimal Amount { get; set; }
        [ProtoMember(4)]
        public Guid SourceWalletId { get; set; }
        [ProtoMember(5)]
        public Guid WalletId { get; set; }        
    }
}
