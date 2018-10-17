using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Orders
{
    /// <summary>
    /// Global settings model
    /// </summary>
    [ProtoContract]
    public class GlobalSettingsModel
    {
        [ProtoMember(1)]
        public string[] BlockedAssetPairs { get; set; }
        [ProtoMember(2)]
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
        [ProtoMember(3)]
        public bool BtcOperationsDisabled { get; set; }
        [ProtoMember(4)]
        public IcoSettingsModel IcoSettings { get; set; }
        [ProtoMember(5)]
        public FeeSettingsModel FeeSettings { get; set; }
    }
}
