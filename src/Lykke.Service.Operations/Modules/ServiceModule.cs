using Autofac;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Repositories;
using Lykke.Service.Operations.Core.Services;
using Lykke.Service.Operations.Repositories;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Services.Blockchain;
using Lykke.Service.Operations.Settings;
using Lykke.Service.Operations.Workflow;
using Lykke.Service.Operations.Workflow.Validation;
using Lykke.SettingsReader;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;

namespace Lykke.Service.Operations.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;

        public ServiceModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterType<OperationsRepository>()
                .As<IOperationsRepository>()
                .SingleInstance();
            
            builder.RegisterInstance<ILimitOrdersRepository>(
                new LimitOrdersRepository(AzureTableStorage<LimitOrderEntity>.Create(_settings.ConnectionString(x => x.OperationsService.Db.HMarketOrdersConnString),
                    "LimitOrders", _log)));
            
            builder.RegisterType<WorkflowService>().As<IWorkflowService>().SingleInstance();

            builder.RegisterType<SrvBlockchainValidations>().SingleInstance();
            builder.RegisterType<EthereumFacade>().As<IEthereumFacade>().SingleInstance();
            builder.RegisterType<SrvNinjaBlockChainReader>()
                .WithParameter("url", _settings.CurrentValue.NinjaServiceClient.ServiceUrl)
                .As<ISrvBlockchainReader>()
                .SingleInstance();

            builder.RegisterInstance<ISrvSolarCoinCommandProducer>(
                new SrvSolarCoinCommandProducer(AzureQueueExt.Create(_settings.ConnectionString(x => x.OperationsService.Db.SolarCoinConnString), "solar-out")));

            builder.RegisterType<BlockchainAddress>().SingleInstance();
            builder.RegisterType<AddressExtensionsCache>()
                .WithParameter("cacheTime", _settings.CurrentValue.OperationsService.BlockchainAddressCacheExpiration)
                .SingleInstance();

            var redis = new RedisCache(new RedisCacheOptions
            {
                Configuration = _settings.CurrentValue.RedisSettings.Configuration,
                InstanceName = "FinanceDataCacheInstance"
            });

            builder.RegisterInstance(redis).As<IDistributedCache>().SingleInstance();
        }
    }
}
