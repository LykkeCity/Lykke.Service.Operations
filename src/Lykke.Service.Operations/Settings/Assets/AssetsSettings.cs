﻿using System;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Operations.Settings.Assets
{
    public class AssetsSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
        public TimeSpan CacheExpirationPeriod { get; set; }
    }
}
