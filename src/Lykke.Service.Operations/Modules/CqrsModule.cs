using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Service.Operations.Contracts.Events;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Settings;
using Lykke.Service.Operations.Workflow.CommandHandlers;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Service.Operations.Workflow.Sagas;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Events;
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
            Messaging.Serialization.MessagePackSerializerFactory.Defaults.FormatterResolver =
                MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSagasSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.CurrentValue.SagasRabbitMq.RabbitConnectionString
            };
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.CurrentValue.Transports.ClientRabbitMqConnectionString
            };

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>();

            builder.RegisterType<MeHandler>().SingleInstance();
            builder.RegisterType<WorkflowCommandHandler>().SingleInstance();

            builder.RegisterType<MeSaga>().SingleInstance();
            builder.RegisterType<BlockchainCashoutSaga>().SingleInstance();

            builder.RegisterInstance(new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "SagasRabbitMq",
                        new TransportInfo(rabbitMqSagasSettings.Endpoint.ToString(), rabbitMqSagasSettings.UserName,
                            rabbitMqSagasSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory())).As<IMessagingEngine>();
          
            var sagasEndpointResolver = new RabbitMqConventionEndpointResolver(
                "SagasRabbitMq",
                "messagepack",
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            var sagasProtobufEndpointResolver = new RabbitMqConventionEndpointResolver(
                "SagasRabbitMq",
                "protobuf",
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            builder.Register(ctx =>
                {
                    return new CqrsEngine(_log,
                        ctx.Resolve<IDependencyResolver>(),
                        ctx.Resolve<IMessagingEngine>(),
                        new DefaultEndpointProvider(),
                        true,
                        Register.DefaultEndpointResolver(sagasEndpointResolver),

                        Register.BoundedContext("operations")
                            .ListeningCommands(typeof(CompleteActivityCommand), typeof(FailActivityCommand)).On("commands")
                                .WithCommandsHandler<WorkflowCommandHandler>()
                            .PublishingEvents(
                                typeof(LimitOrderCreatedEvent),
                                typeof(LimitOrderRejectedEvent),
                                typeof(OperationCreatedEvent),
                                typeof(ExternalExecutionActivityCreatedEvent))                            
                            .With("events"),

                        Register.BoundedContext(SwiftWithdrawalBoundedContext.Name)
                            .PublishingCommands(typeof(SwiftCashoutCreateCommand))                            
                            .To(SwiftWithdrawalBoundedContext.Name)                            
                            .With("commands"),

                        Register.BoundedContext("me")
                            .ListeningCommands(typeof(MeCashoutCommand)).On("commands")
                                .WithCommandsHandler(ctx.Resolve<MeHandler>())
                            .PublishingEvents(typeof(MeCashoutFailedEvent)).With("events"),

                        Register.Saga<MeSaga>("me-saga")
                            .ListeningEvents(typeof(ExternalExecutionActivityCreatedEvent))
                                .From("operations").On("events")                            
                            .ListeningEvents(typeof(CashOutProcessedEvent))
                                .From("post-processing").On("events")
                                .WithEndpointResolver(sagasProtobufEndpointResolver)
                            .ListeningEvents(typeof(MeCashoutFailedEvent))
                                .From("me").On("events")
                            .PublishingCommands(typeof(MeCashoutCommand))
                                .To("me").With("commands")
                            .PublishingCommands(typeof(CompleteActivityCommand), typeof(FailActivityCommand))
                                .To("operations").With("commands"),

                        Register.Saga<BlockchainCashoutSaga>("blockchain-cashout-saga")
                            .ListeningEvents(typeof(ExternalExecutionActivityCreatedEvent))
                                .From("operations").On("events")
                            .ListeningEvents(typeof(OperationExecutionCompletedEvent), typeof(OperationExecutionFailedEvent))
                                .From(BlockchainOperationsExecutorBoundedContext.Name).On("events")
                            .PublishingCommands(typeof(StartCashoutCommand))
                                .To(BlockchainCashoutProcessorBoundedContext.Name).With("commands")
                            .PublishingCommands(typeof(CompleteActivityCommand))
                                .To("operations").With("commands")
                    );
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
