using System;
using Autofac;
using AzureStorage.Queue;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Sdk;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Domain.MyNoSqlEntities;
using Lykke.Service.Operations.Core.Repositories;
using Lykke.Service.Operations.Core.Services;
using Lykke.Service.Operations.PeriodicalHandlers;
using Lykke.Service.Operations.Repositories;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Services.Blockchain;
using Lykke.Service.Operations.Settings;
using Lykke.Service.Operations.Workflow.Validation;
using Lykke.SettingsReader;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;

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
            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<OperationsRepository>()
                .As<IOperationsRepository>()
                .SingleInstance();
            builder.Register<ILimitOrdersRepository>(ctx =>
                new LimitOrdersRepository(
                    AzureTableStorage<LimitOrderEntity>.Create(
                        _settings.ConnectionString(x => x.OperationsService.Db.HMarketOrdersConnString),
                        "LimitOrders",
                        ctx.Resolve<ILogFactory>(),
                        TimeSpan.FromSeconds(30))))
                .SingleInstance();

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

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<OperationEntity>(() =>
                    _settings.CurrentValue.MyNoSqlServer.WriterUrl, "operations");
            }).As<IMyNoSqlServerDataWriter<OperationEntity>>().SingleInstance();

            builder.Register(ctx =>
            {
                return new MyNoSqlServer.DataWriter.MyNoSqlServerDataWriter<OperationIndexEntity>(() =>
                    _settings.CurrentValue.MyNoSqlServer.WriterUrl, "operationindexes");
            }).As<IMyNoSqlServerDataWriter<OperationIndexEntity>>().SingleInstance();

            builder.Register(ctx =>
                {
                    var client = new MyNoSqlTcpClient(() => _settings.CurrentValue.MyNoSqlServer.ReaderUrl,
                        $"operations-{Environment.MachineName}");
                    client.Start();
                    return client;
                })
                .AsSelf()
                .SingleInstance();

            builder.Register(ctx =>
                    new MyNoSqlReadRepository<OperationEntity>(ctx.Resolve<MyNoSqlTcpClient>(),
                        "operations")
                )
                .As<IMyNoSqlServerDataReader<OperationEntity>>()
                .SingleInstance();

            builder.Register(ctx =>
                    new MyNoSqlReadRepository<OperationIndexEntity>(ctx.Resolve<MyNoSqlTcpClient>(),
                        "operationindexes")
                )
                .As<IMyNoSqlServerDataReader<OperationIndexEntity>>()
                .SingleInstance();

            builder.RegisterType<OperationsCacheService>().As<IOperationsCacheService>().SingleInstance();

            builder.RegisterType<OperationsHandler>()
                .As<IStartable>()
                .AutoActivate()
                .SingleInstance();
        }
    }
}
