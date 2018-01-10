using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.Operations.Core.Settings.Assets;
using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.PushNotifications.Client.AutorestClient;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Operations.Modules
{
    public class ClientsModule : Module
    {
        private readonly IServiceCollection _services;
        private readonly OperationsSettings _settings;
        private readonly AssetsSettings _assetSettings;

        public ClientsModule(OperationsSettings settings, AssetsSettings assetSettings)
        {
            _settings = settings;
            _assetSettings = assetSettings;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .WithParameter("baseUri", new Uri(_settings.Services.ClientAccountUrl));

            builder.RegisterType<PushNotificationsAPI>()
                .As<IPushNotificationsAPI>()
                .WithParameter("baseUri", new Uri(_settings.Services.PushNotificationsUrl));

            _services.RegisterAssetsClient(new AssetServiceSettings
            {
                AssetsCacheExpirationPeriod = _assetSettings.CacheExpirationPeriod,
                BaseUri = new Uri(_assetSettings.ServiceUrl)
            });

            builder.Populate(_services);
        }
    }
}
