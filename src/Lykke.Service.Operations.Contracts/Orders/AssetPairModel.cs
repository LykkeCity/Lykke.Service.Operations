using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Orders
{
    /// <summary>
    /// Asset pair model
    /// </summary>
    [ProtoContract]
    public class AssetPairModel
    {
        [ProtoMember(1)]
        public string Id { get; set; }
        [ProtoMember(2)]
        public AssetModel BaseAsset { get; set; }
        [ProtoMember(3)]
        public AssetModel QuotingAsset { get; set; }
        [ProtoMember(4)]
        public double MinVolume { get; set; }
        [ProtoMember(5)]
        public double MinInvertedVolume { get; set; }
    }
}
