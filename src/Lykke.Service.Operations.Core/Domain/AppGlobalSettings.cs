using System;

namespace Lykke.Service.Operations.Core.Domain
{
    public interface IAppGlobalSettings
    {
        string DepositUrl { get; }
        bool DebugMode { get; }
        string DefaultIosAssetGroup { get; set; }
        string DefaultAssetGroupForOther { get; set; }
        bool IsOnReview { get; }
        double? MinVersionOnReview { get; }
        double IcoLkkSold { get; }
        bool IsOnMaintenance { get; }
        int LowCashOutTimeoutMins { get; }
        int LowCashOutLimit { get; }
        bool MarginTradingEnabled { get; }
        bool CashOutBlocked { get; }
        bool BtcOperationsDisabled { get; }
        bool BitcoinBlockchainOperationsDisabled { get; }
        bool LimitOrdersEnabled { get; }
        double MarketOrderPriceDeviation { get; }
        string[] BlockedAssetPairs { get; set; }
        string OnReviewAssetConditionLayer { get; set; }
        DateTime? IcoStartDtForWhitelisted { get; set; }
        DateTime? IcoStartDt { get; set; }
        bool ShowIcoBanner { get; set; }
    }

    public class AppGlobalSettings : IAppGlobalSettings
    {
        public static AppGlobalSettings CreateDefault()
        {
            return new AppGlobalSettings
            {
                DepositUrl = "http://mock-bankcards.azurewebsites.net/",
                DebugMode = true
            };
        }

        public string DepositUrl { get; set; }
        public bool DebugMode { get; set; }
        public string DefaultIosAssetGroup { get; set; }
        public string DefaultAssetGroupForOther { get; set; }
        public bool IsOnReview { get; set; }
        public double? MinVersionOnReview { get; set; }
        public double IcoLkkSold { get; set; }
        public bool IsOnMaintenance { get; set; }
        public int LowCashOutTimeoutMins { get; set; }
        public int LowCashOutLimit { get; set; }
        public bool MarginTradingEnabled { get; set; }
        public bool CashOutBlocked { get; set; }
        public bool BtcOperationsDisabled { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
        public bool LimitOrdersEnabled { get; set; }
        public double MarketOrderPriceDeviation { get; set; }
        public string[] BlockedAssetPairs { get; set; }
        public string OnReviewAssetConditionLayer { get; set; }
        public DateTime? IcoStartDtForWhitelisted { get; set; }
        public DateTime? IcoStartDt { get; set; }
        public bool ShowIcoBanner { get; set; }
    }
}
