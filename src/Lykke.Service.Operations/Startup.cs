using System;
using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.Operations.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.Operations
{

    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "Operations API",
            ApiVersion = "v1"
        };

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
                
                options.Logs = logs =>
                {
                    logs.AzureTableName = "OperationsLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.OperationsService.Db.LogsConnString;
                };                
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;                
            });
        }
    }
}
