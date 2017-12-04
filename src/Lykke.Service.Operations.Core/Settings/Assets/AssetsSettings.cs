using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Operations.Core.Settings.Assets
{
    public class AssetsSettings
    {
        public string ServiceUrl { get; set; }
        public TimeSpan CacheExpirationPeriod { get; set; }
    }
}
