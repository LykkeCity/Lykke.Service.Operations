using System;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Sdk;
using Lykke.Service.Operations.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using GlobalErrorHandlerMiddleware = Lykke.Service.Operations.Middleware.GlobalErrorHandlerMiddleware;

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
            CreateErrorResponse errorResponseFactory = ex => new { ex.Message };

            app.UseLykkeConfiguration(options =>
            {
                options.WithMiddleware = x => x.UseMiddleware<GlobalErrorHandlerMiddleware>("Operations", errorResponseFactory);
                options.SwaggerOptions = _swaggerOptions;
            });
        }
    }
}
