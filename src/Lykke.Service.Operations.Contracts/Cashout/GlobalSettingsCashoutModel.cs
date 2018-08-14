namespace Lykke.Service.Operations.Contracts.Cashout
{
    public class GlobalSettingsCashoutModel
    {
        public bool CashOutBlocked { get; set; }
        public string EthereumHotWallet { get; set; }

        public FeeSettingsCashoutModel FeeSettings { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }
}
