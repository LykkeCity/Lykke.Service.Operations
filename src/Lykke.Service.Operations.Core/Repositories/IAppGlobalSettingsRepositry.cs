using System;
using System.Threading.Tasks;
using Lykke.Service.Operations.Core.Domain;

namespace Lykke.Service.Operations.Core.Repositories
{
    public interface IAppGlobalSettingsRepositry
    {
        Task SaveAsync(IAppGlobalSettings appGlobalSettings);

        Task UpdateAsync(string depositUrl = null, bool? debugMode = null,
            string defaultIosAssetGroup = null, string defaultAssetGroupForOther = null,
            double? minVersionOnReview = null, bool? isOnReview = null,
            double? icoLkkSold = null, bool? isOnMaintenance = null, int? lowCashOutTimeout = null,
            int? lowCashOutLimit = null, bool? marginTradingEnabled = null,
            bool? cashOutBlocked = null, bool? btcDisabled = null, bool? btcBlockchainDisabled = null,
            bool? limitOrdersEnabled = null, double? marketOrdersDeviation = null, string[] blockedAssetPairs = null,
            string onReviewAssetConditionLayer = null,
            DateTime? icoStartDtForWhitelisted = null, DateTime? icoStartDt = null, bool? showIcoBanner = null);

        Task<IAppGlobalSettings> GetAsync();
    }
}
