namespace Lykke.Service.Operations.Settings.ServiceSettings
{
    public class OperationsSettings
    {
        public DbSettings Db { get; set; }

        public ServicesSettings Services { get; set; }
    }

    public class ServicesSettings
    {
        public string ClientAccountUrl { get; set; }
        public string PushNotificationsUrl { get; set; }
    }
}
