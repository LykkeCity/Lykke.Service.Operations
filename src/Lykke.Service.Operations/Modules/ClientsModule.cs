using System;
using System.Net.Http;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Common.Log;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.EthereumCore.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
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
        private readonly IServiceCollection _services;

        public ClientsModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
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

            builder.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CurrentValue.Assets.ServiceUrl),
                _settings.CurrentValue.Assets.CacheExpirationPeriod
            ));

            builder.RegisterInstance(_settings.CurrentValue.EthereumServiceClient);

            builder.RegisterInstance<IEthereumCoreAPI>(new EthereumCoreAPI(new Uri(_settings.CurrentValue.EthereumServiceClient.ServiceUrl), new HttpClient()));

            builder.Register(ctx => new BlockchainCashoutPreconditionsCheckClient(
                    _settings.CurrentValue.BlockchainCashoutPreconditionsCheckServiceClient.ServiceUrl,
                    ctx.Resolve<ILogFactory>().CreateLog("BlockchainCashoutPreconditionsCheckClient")))
                .As<IBlockchainCashoutPreconditionsCheckClient>()
                .SingleInstance();


            builder.Register(ctx => new BlockchainWalletsClient(
                    _settings.CurrentValue.BlockchainWalletsServiceClient.ServiceUrl,
                    ctx.Resolve<ILogFactory>().CreateLog("BlockchainWalletsClient")))
                .As<IBlockchainWalletsClient>()
                .SingleInstance();

            builder.RegisterRateCalculatorClient(_settings.CurrentValue.RateCalculatorServiceClient.ServiceUrl);

            builder.Register(ctx => new BalancesClient(
                    _settings.CurrentValue.BalancesServiceClient.ServiceUrl,
                    ctx.Resolve<ILogFactory>().CreateLog("BalancesClient")))
                .As<IBalancesClient>()
                .SingleInstance();

            builder.RegisterFeeCalculatorClient(_settings.CurrentValue.FeeCalculatorServiceClient.ServiceUrl);

            builder.RegisterInstance<IAssetDisclaimersClient>(new AssetDisclaimersClient(_settings.CurrentValue.AssetDisclaimersServiceClient));
            builder.BindMeClient(_settings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint(), socketLog: null, ignoreErrors: true);
            builder.RegisterLimitationsServiceClient(_settings.CurrentValue.LimitationServiceClient.ServiceUrl);
            builder.RegisterInstance<IExchangeOperationsServiceClient>(new ExchangeOperationsServiceClient(_settings.CurrentValue.ExchangeOperationsServiceClient.ServiceUrl));

            builder.Populate(_services);
        }
    }
}
