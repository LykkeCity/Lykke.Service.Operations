using Lykke.Service.Operations.Core.Settings.Assets;
using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.Operations.Core.Settings.SlackNotifications;

namespace Lykke.Service.Operations.Core.Settings
{
    public class AppSettings
    {
        public OperationsSettings OperationsService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public AssetsSettings Assets { get; set; }
        public RateCalculatorSettings RateCalculatorServiceClient { get; set; }
        public BalancesSettings BalancesServiceClient { get; set; }
    }

    public class BalancesSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class RateCalculatorSettings
    {
        public string ServiceUrl { get; set; }
    }
}
