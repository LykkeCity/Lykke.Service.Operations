using Lykke.Service.Operations.Contracts.Orders;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Commands
{
    /// <summary>
    /// Command to create limit order
    /// </summary>
    [ProtoContract]
    public class CreateLimitOrderCommand
    {
        [ProtoMember(1)]
        public AssetPairModel AssetPair { get; set; }
        [ProtoMember(2)]
        public double Volume { get; set; }
        [ProtoMember(3)]
        public decimal Price { get; set; }
        [ProtoMember(4)]
        public OrderAction OrderAction { get; set; }
        [ProtoMember(5)]
        public ClientModel Client { get; set; }
        [ProtoMember(6)]
        public GlobalSettingsModel GlobalSettings { get; set; }
    }
}
