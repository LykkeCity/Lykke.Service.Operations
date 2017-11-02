using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.Operations.Core.Settings.SlackNotifications;

namespace Lykke.Service.Operations.Core.Settings
{
    public class AppSettings
    {
        public OperationsSettings OperationsService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
