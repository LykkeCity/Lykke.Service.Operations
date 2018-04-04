using System;

namespace Lykke.Service.Operations.Settings.Assets
{
    public class AssetsSettings
    {
        public string ServiceUrl { get; set; }
        public TimeSpan CacheExpirationPeriod { get; set; }
    }
}
