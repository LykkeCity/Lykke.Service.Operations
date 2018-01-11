using Autofac;
using Common.Log;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Services;
using Lykke.Service.Operations.MongoRepositories;
using Lykke.Service.Operations.Services;

namespace Lykke.Service.Operations.Modules
{
    public class ServiceModule : Module
    {
        private readonly ILog _log;

        public ServiceModule(ILog log)
        {
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
                .As<IOperationsRepository>();
        }
    }
}
