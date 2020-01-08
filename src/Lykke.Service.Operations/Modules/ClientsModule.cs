using System;
using System.Net.Http;
using Autofac;
using Lykke.Common.Log;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Client;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Client.ClientGenerator;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.EthereumCore.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Kyc.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Settings;
using Lykke.Service.PushNotifications.Client.AutorestClient;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;

namespace Lykke.Service.Operations.Modules
{
    public class ClientsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public ClientsModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterClientAccountClient(_settings.CurrentValue.OperationsService.Services.ClientAccountUrl);

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
                    _settings.CurrentValue.BlockchainCashoutPreconditionsCheckServiceClient.ServiceUrl))
                .As<IBlockchainCashoutPreconditionsCheckClient>()
                .SingleInstance();

            builder.Register(ctx => new BlockchainWalletsClient(
                    _settings.CurrentValue.BlockchainWalletsServiceClient.ServiceUrl,
                    ctx.Resolve<ILogFactory>(),
                    new BlockchainWalletsApiFactory()))
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
            builder.RegisterMeClient(_settings.CurrentValue.MatchingEngineClient.IpEndpoint.GetClientIpEndPoint(), true);
            builder.RegisterLimitationsServiceClient(_settings.CurrentValue.LimitationServiceClient.ServiceUrl);
            builder.RegisterExchangeOperationsClient(_settings.CurrentValue.ExchangeOperationsServiceClient.ServiceUrl);

            builder.Register(ctx =>
                    new KycStatusServiceClient(_settings.CurrentValue.KycServiceClient, ctx.Resolve<ILogFactory>()))
                .As<IKycStatusService>()
                .SingleInstance();
        }
    }
}
