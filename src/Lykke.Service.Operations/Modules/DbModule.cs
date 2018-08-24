using Autofac;
using Lykke.Service.Operations.Settings;
using Lykke.Service.Operations.Settings.ServiceSettings;
using Lykke.SettingsReader;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Lykke.Service.Operations.Modules
{
    public class DbModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;
        
        public DbModule(IReloadingManager<AppSettings> dbSettings)
        {
            _dbSettings = dbSettings.Nested(a => a.OperationsService.Db);
        }

        protected override void Load(ContainerBuilder builder)
        {
            var mongoUrl = new MongoUrl(_dbSettings.CurrentValue.MongoConnectionString);
            ConventionRegistry.Register("Ignore extra", new ConventionPack { new IgnoreExtraElementsConvention(true) }, x => true);

            var database = new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName);
            builder.RegisterInstance(database);
        }
    }
}
