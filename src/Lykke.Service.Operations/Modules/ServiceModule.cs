using Autofac;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.Log;
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

        public ServiceModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<OperationsRepository>()
                .As<IOperationsRepository>()
                .SingleInstance();
            builder.Register<ILimitOrdersRepository>(ctx =>
                new LimitOrdersRepository(AzureTableStorage<LimitOrderEntity>.Create(_settings.ConnectionString(x => x.OperationsService.Db.HMarketOrdersConnString),
                    "LimitOrders", ctx.Resolve<ILogFactory>()))).SingleInstance();
            
            builder.RegisterType<WorkflowService>().As<IWorkflowService>().SingleInstance();
            
            builder.RegisterType<EthereumFacade>().As<IEthereumFacade>().SingleInstance();            

            builder.RegisterInstance<ISrvSolarCoinCommandProducer>(
                new SrvSolarCoinCommandProducer(AzureQueueExt.Create(_settings.ConnectionString(x => x.OperationsService.Db.SolarCoinConnString), "solar-out")));

            builder.RegisterType<BlockchainAddress>().SingleInstance();
            builder.RegisterType<AddressExtensionsCache>()
                .WithParameter("cacheTime", _settings.CurrentValue.OperationsService.BlockchainAddressCacheExpiration)
                .SingleInstance();

            var redis = new RedisCache(new RedisCacheOptions
            {
                Configuration = _settings.CurrentValue.RedisSettings.Configuration,
                InstanceName = "AddressExtensionsInstance"
            });

            builder.RegisterInstance(redis).As<IDistributedCache>().SingleInstance();
        }
    }
}
