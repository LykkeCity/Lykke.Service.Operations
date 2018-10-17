using Lykke.Service.Operations.Contracts.Orders;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Commands
{
    /// <summary>
    /// Command to create market order
    /// </summary>
    [ProtoContract]
    public class CreateMarketOrderCommand
    {
        [ProtoMember(1)]
        public string AssetId { get; set; }
        [ProtoMember(2)]
        public AssetPairModel AssetPair { get; set; }
        [ProtoMember(3)]
        public double Volume { get; set; }
        [ProtoMember(4)]
        public OrderAction OrderAction { get; set; }
        [ProtoMember(5)]
        public ClientModel Client { get; set; }
        [ProtoMember(6)]
        public GlobalSettingsModel GlobalSettings { get; set; }        
    }
}
