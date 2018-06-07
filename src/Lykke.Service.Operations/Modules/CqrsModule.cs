using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Settings;
using Lykke.Service.SwiftWithdrawal.Contracts;
using Lykke.SettingsReader;

namespace Lykke.Service.Operations.Modules
{
    public class CqrsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;

        public CqrsModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            Messaging.Serialization.MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSagasSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.CurrentValue.SagasRabbitMq.RabbitConnectionString };
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _settings.CurrentValue.Transports.ClientRabbitMqConnectionString };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>();

            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    { "SagasRabbitMq", new TransportInfo(rabbitMqSagasSettings.Endpoint.ToString(), rabbitMqSagasSettings.UserName, rabbitMqSagasSettings.Password, "None", "RabbitMq") },
                    { "ClientRabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq") }
                }),
                new RabbitMqTransportFactory());

            var sagasEndpointResolver = new RabbitMqConventionEndpointResolver(
                "SagasRabbitMq",
                "messagepack",
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            var clientEndpointResolver = new RabbitMqConventionEndpointResolver(
                "ClientRabbitMq",
                "messagepack",
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            builder.Register(ctx =>
                {
                    return new CqrsEngine(_log,
                        ctx.Resolve<IDependencyResolver>(),
                        messagingEngine,
                        new DefaultEndpointProvider(),
                        true,
                        Register.DefaultEndpointResolver(clientEndpointResolver),

                        Register.BoundedContext("operations")
                            .PublishingEvents(typeof(LimitOrderCreatedEvent), typeof(LimitOrderRejectedEvent), typeof(OperationCreatedEvent)).With("events"),

                        Register.BoundedContext(SwiftWithdrawalBoundedContext.Name)
                            .PublishingCommands(typeof(SwiftCashoutCreateCommand))
                            .To(SwiftWithdrawalBoundedContext.Name)
                            .With("commands")
                            .WithEndpointResolver(sagasEndpointResolver));

                })
            .As<ICqrsEngine>()
            .SingleInstance()
            .AutoActivate();
        }

        internal class AutofacDependencyResolver : IDependencyResolver
        {
            private readonly IComponentContext _context;

            public AutofacDependencyResolver([NotNull] IComponentContext kernel)
            {
                _context = kernel ?? throw new ArgumentNullException(nameof(kernel));
            }

            public object GetService(Type type)
            {
                return _context.Resolve(type);
            }

            public bool HasService(Type type)
            {
                return _context.IsRegistered(type);
            }
        }
    }
}
