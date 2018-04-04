using Autofac;
using Lykke.Service.Operations.Settings.ServiceSettings;
using Lykke.SettingsReader;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Lykke.Service.Operations.Modules
{
    public class MongoDbModule : Module
    {
        private readonly IReloadingManager<DbSettings> _dbSettings;

        public MongoDbModule(IReloadingManager<DbSettings> dbSettings)
        {
            _dbSettings = dbSettings;
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
