using System;
using Autofac;
using JetBrains.Annotations;
using Lykke.HttpClientGenerator.Infrastructure;

namespace Lykke.Service.Operations.Client
{
    [PublicAPI]
    public static class AutofacExtension
    {
        public static void RegisterOperationsClient(this ContainerBuilder builder, string serviceUrl)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            var httpClientGenerator = HttpClientGenerator.HttpClientGenerator
                .BuildForUrl(serviceUrl)
                .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper())
                .WithoutRetries()
                .Create();

            builder.RegisterInstance(new OperationsServiceClient(httpClientGenerator))
                .As<IOperationsClient>()
                .SingleInstance();
        }

        public static void RegisterOperationsClient(this ContainerBuilder builder, OperationsServiceClientSettings settings)
        {
            builder.RegisterOperationsClient(settings?.ServiceUrl);
        }
    }
}
