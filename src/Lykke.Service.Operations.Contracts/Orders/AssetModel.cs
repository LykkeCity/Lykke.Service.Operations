using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Orders
{
    /// <summary>
    /// Asset model
    /// </summary>
    [ProtoContract]
    public class AssetModel
    {
        [ProtoMember(1)]
        public string Id { get; set; }
        [ProtoMember(2)]
        public string DisplayId { get; set; }
        [ProtoMember(3)]
        public int Accuracy { get; set; }
        [ProtoMember(4)]
        public bool IsTradable { get; set; }
        [ProtoMember(5)]
        public bool IsTrusted { get; set; }
        [ProtoMember(6)]
        public bool KycNeeded { get; set; }
        [ProtoMember(7)]
        public string LykkeEntityId { get; set; }
        [ProtoMember(8)]
        public string Blockchain { get; set; }
    }
}
