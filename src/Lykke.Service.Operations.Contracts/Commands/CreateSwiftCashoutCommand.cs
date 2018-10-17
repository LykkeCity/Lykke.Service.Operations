using System;
using Lykke.Service.Operations.Contracts.SwiftCashout;
using MessagePack;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Commands
{
    /// <summary>
    /// Command to create swift cashout
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    [ProtoContract]
    public class CreateSwiftCashoutCommand
    {
        [ProtoMember(1)]
        public Guid OperationId { get; set; }
        [ProtoMember(2)]
        public SwiftCashoutAssetModel Asset { get; set; }
        [ProtoMember(3)]
        public decimal Volume { get; set; }
        [ProtoMember(4)]
        public SwiftCashoutClientModel Client { get; set; }
        [ProtoMember(5)]
        public SwiftFieldsModel Swift { get; set; }
        [ProtoMember(6)]
        public SwiftCashoutSettingsModel CashoutSettings { get; set; }
    }
}
