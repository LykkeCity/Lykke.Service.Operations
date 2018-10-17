using MessagePack;

namespace Lykke.Service.Operations.Contracts.Cashout
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class GlobalSettingsCashoutModel
    {
        public bool CashOutBlocked { get; set; }
        public string EthereumHotWallet { get; set; }

        public FeeSettingsCashoutModel FeeSettings { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public int MaxConfirmationAttempts { get; set; }
    }
}
