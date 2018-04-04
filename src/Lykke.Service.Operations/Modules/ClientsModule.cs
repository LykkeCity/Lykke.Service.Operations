using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Operations.Settings;
using Lykke.Service.PushNotifications.Client.AutorestClient;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Operations.Modules
{
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;
        private readonly IServiceCollection _services;        

        public ClientsModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .WithParameter("baseUri", new Uri(_settings.CurrentValue.OperationsService.Services.ClientAccountUrl));

            builder.RegisterType<PushNotificationsAPI>()
                .As<IPushNotificationsAPI>()
                .WithParameter("baseUri", new Uri(_settings.CurrentValue.OperationsService.Services.PushNotificationsUrl));

            _services.RegisterAssetsClient(new AssetServiceSettings
            {
                AssetsCacheExpirationPeriod = _settings.CurrentValue.Assets.CacheExpirationPeriod,
                BaseUri = new Uri(_settings.CurrentValue.Assets.ServiceUrl)
            });

            builder.RegisterRateCalculatorClient(_settings.CurrentValue.RateCalculatorServiceClient.ServiceUrl, _log);
            builder.RegisterBalancesClient(_settings.CurrentValue.BalancesServiceClient.ServiceUrl, _log);
            builder.RegisterFeeCalculatorClient(_settings.CurrentValue.FeeCalculatorServiceClient.ServiceUrl, _log);
            builder.RegisterInstance<IAssetDisclaimersClient>(new AssetDisclaimersClient(_settings.CurrentValue.AssetDisclaimersServiceClient));
            builder.BindMeClient(_settings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint(), socketLog: null, ignoreErrors: true);

            builder.Populate(_services);
        }
    }
}
