using MessagePack;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Cashout
{
    [MessagePackObject(keyAsPropertyName: true)]
    [ProtoContract]
    public class AssetCashoutModel
    {
        [ProtoMember(1)]
        public string Id { get; set; }
        [ProtoMember(2)]
        public string DisplayId { get; set; }
        [ProtoMember(3)]
        public int MultiplierPower { get; set; }
        [ProtoMember(4)]
        public string AssetAddress { get; set; }
        [ProtoMember(5)]
        public int Accuracy { get; set; }
        [ProtoMember(6)]
        public string Blockchain { get; set; }
        [ProtoMember(7)]
        public string Type { get; set; }
        [ProtoMember(8)]
        public bool IsTradable { get; set; }
        [ProtoMember(9)]
        public bool IsTrusted { get; set; }
        [ProtoMember(10)]
        public bool KycNeeded { get; set; }
        [ProtoMember(11)]
        public string BlockchainIntegrationLayerId { get; set; }
        [ProtoMember(12)]
        public decimal CashoutMinimalAmount { get; set; }
        [ProtoMember(13)]
        public decimal LowVolumeAmount { get; set; }
        [ProtoMember(14)]
        public bool BlockchainWithdrawal { get; set; }
        [ProtoMember(15)]
        public string LykkeEntityId { get; set; }
    }
}
