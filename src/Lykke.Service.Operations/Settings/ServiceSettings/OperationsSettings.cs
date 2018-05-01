using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Operations.Settings.ServiceSettings
{
    public class OperationsSettings
    {
        public DbSettings Db { get; set; }

        public ServicesSettings Services { get; set; }
    }

    public class ServicesSettings
    {
        [HttpCheck("/api/isalive")]
        public string ClientAccountUrl { get; set; }
        [HttpCheck("/api/isalive")]
        public string PushNotificationsUrl { get; set; }
    }
}
