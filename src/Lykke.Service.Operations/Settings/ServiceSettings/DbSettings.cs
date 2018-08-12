using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Operations.Settings.ServiceSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }

        public string MongoConnectionString { get; set; }

        [AzureTableCheck]
        public string HMarketOrdersConnString { get; set; }

        public string SolarCoinConnString { get; set; }
        [AzureTableCheck]
        public string ClientPersonalInfoConnString { get; set; }
    }
}
