using MessagePack;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Cashout
{
    [MessagePackObject(keyAsPropertyName: true)]
    [ProtoContract]
    public class GlobalSettingsCashoutModel
    {
        [ProtoMember(1)]
        public bool CashOutBlocked { get; set; }
        [ProtoMember(2)]
        public string EthereumHotWallet { get; set; }
        [ProtoMember(3)]
        public FeeSettingsCashoutModel FeeSettings { get; set; }
        [ProtoMember(4)]
        public bool TwoFactorEnabled { get; set; }
        [ProtoMember(5)]
        public int MaxConfirmationAttempts { get; set; }
    }
}
