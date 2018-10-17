using System;
using Lykke.Service.Operations.Contracts.Cashout;
using MessagePack;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    [ProtoContract]
    public class CreateCashoutCommand
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }
        [ProtoMember(2)]
        public string DestinationAddress { get; set; }
        [ProtoMember(3)]
        public string DestinationAddressExtension { get; set; }
        [ProtoMember(4)]
        public decimal Volume { get; set; }
        [ProtoMember(5)]
        public AssetCashoutModel Asset { get; set; }
        [ProtoMember(6)]
        public ClientCashoutModel Client { get; set; }
        [ProtoMember(7)]
        public GlobalSettingsCashoutModel GlobalSettings { get; set; }
    }
}
