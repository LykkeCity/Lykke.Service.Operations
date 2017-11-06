using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.Log;
using Lykke.Service.ClientAccount.Client.AutorestClient;
using Lykke.Service.Operations.AzureRepositories;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Services;
using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.Operations.Services;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Operations.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<OperationsSettings> _settings;
        private readonly IReloadingManager<ServicesSettings> _servicesSettings;
        private readonly IReloadingManager<DbSettings> _dbSettings;

        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public ServiceModule(IReloadingManager<OperationsSettings> settings, IReloadingManager<DbSettings> dbSettings, IReloadingManager<ServicesSettings> servicesSettings, ILog log)
        {
            _settings = settings;
            _servicesSettings = servicesSettings;
            _dbSettings = dbSettings;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            // TODO: Do not register entire settings in container, pass necessary settings to services which requires them
            // ex:
            //  builder.RegisterType<QuotesPublisher>()
            //      .As<IQuotesPublisher>()
            //      .WithParameter(TypedParameter.From(_settings.CurrentValue.QuotesPublication))

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

            builder.RegisterInstance<IOperationsRepository>(new OperationsRepository(
                AzureTableStorage<OperationEntity>.Create(_dbSettings.ConnectionString(x => x.OperationsConnectionString), "Operations", _log),
                AzureTableStorage<AzureIndex>.Create(_dbSettings.ConnectionString(x => x.OperationsConnectionString), "Operations", _log)));

            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .WithParameter("baseUri", new Uri(_servicesSettings.Nested(s => s.ClientAccountUrl).CurrentValue));

            builder.Populate(_services);
        }
    }
}
