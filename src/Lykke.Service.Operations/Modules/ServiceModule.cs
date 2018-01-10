using Autofac;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.Log;
using Lykke.Service.Operations.AzureRepositories;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Services;
using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.Operations.Services;
using Lykke.SettingsReader;

namespace Lykke.Service.Operations.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;

        private readonly ILog _log;

        public ServiceModule(IReloadingManager<DbSettings> dbSettings, ILog log)
        {
            _dbSettings = dbSettings;
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

            builder.RegisterInstance<IOperationsRepository>(new OperationsRepository(
                AzureTableStorage<OperationEntity>.Create(_dbSettings.ConnectionString(x => x.OperationsConnectionString), "Operations", _log),
                AzureTableStorage<AzureIndex>.Create(_dbSettings.ConnectionString(x => x.OperationsConnectionString), "Operations", _log)));
        }
    }
}
