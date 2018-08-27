using System;
using System.Collections.Generic;
using Autofac;
using JetBrains.Annotations;
using Lykke.Bitcoin.Contracts;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;
using Lykke.Job.EthereumCore.Contracts.Cqrs;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.Service.Operations.Contracts.Events;
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
        
        public CqrsModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            Messaging.Serialization.MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;
            var rabbitMqSagasSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = _settings.CurrentValue.SagasRabbitMq.RabbitConnectionString
            };
            
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>();

            builder.RegisterType<WorkflowCommandHandler>().SingleInstance();
            builder.RegisterType<CommandHandler>()
                .WithParameter("ethereumHotWallet", _settings.CurrentValue.EthereumServiceClient.HotwalletAddress)
                .SingleInstance();
            builder.RegisterType<SolarCoinCommandHandler>().SingleInstance();

            builder.RegisterType<MeSaga>().SingleInstance();
            builder.RegisterType<BlockchainCashoutSaga>().SingleInstance();
            builder.RegisterType<WorkflowSaga>().SingleInstance();

            builder.Register(ctx => new MessagingEngine(ctx.Resolve<ILogFactory>(),
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {
                        "SagasRabbitMq",
                        new TransportInfo(rabbitMqSagasSettings.Endpoint.ToString(), rabbitMqSagasSettings.UserName,
                            rabbitMqSagasSettings.Password, "None", "RabbitMq")
                    }
                }),
                new RabbitMqTransportFactory(ctx.Resolve<ILogFactory>()))).As<IMessagingEngine>().SingleInstance();
          
            var sagasEndpointResolver = new RabbitMqConventionEndpointResolver(
                "SagasRabbitMq",
                SerializationFormat.MessagePack,
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            var sagasProtobufEndpointResolver = new RabbitMqConventionEndpointResolver(
                "SagasRabbitMq",
                SerializationFormat.ProtoBuf,
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            builder.Register(ctx =>
                {
                    return new CqrsEngine(
                        ctx.Resolve<ILogFactory>(),
                        ctx.Resolve<IDependencyResolver>(),
                        ctx.Resolve<IMessagingEngine>(),
                        new DefaultEndpointProvider(),
                        true,
                        Register.DefaultEndpointResolver(sagasEndpointResolver),

                        Register.BoundedContext("operations")
                            .ListeningCommands(
                                typeof(CreateCashoutCommand))
                                .On("commands")                                
                                .WithCommandsHandler<CommandHandler>()
                            .ListeningCommands(
                                typeof(ExecuteOperationCommand),
                                typeof(CompleteActivityCommand), 
                                typeof(FailActivityCommand))
                                .On("commands")
                                .WithLoopback()
                                .WithCommandsHandler<WorkflowCommandHandler>()                            
                            .PublishingEvents(
                                typeof(LimitOrderCreatedEvent),
                                typeof(LimitOrderRejectedEvent),
                                typeof(OperationCreatedEvent),
                                typeof(OperationCorruptedEvent),
                                typeof(OperationFailedEvent),
                                typeof(ExternalExecutionActivityCreatedEvent))                            
                                .With("events")
                                .WithLoopback(),

                        Register.BoundedContext("solarcoin")
                            .ListeningCommands(typeof(SolarCashOutCommand)).On("commands")
                                .WithCommandsHandler<SolarCoinCommandHandler>()
                            .PublishingEvents(typeof(SolarCashOutCompletedEvent))
                                .With("events"),

                        Register.BoundedContext(SwiftWithdrawalBoundedContext.Name)
                            .PublishingCommands(typeof(SwiftCashoutCreateCommand))                            
                            .To(SwiftWithdrawalBoundedContext.Name)                            
                            .With("commands"),
                        
                        Register.Saga<MeSaga>("me-saga")
                            .ListeningEvents(typeof(ExternalExecutionActivityCreatedEvent))
                                .From("operations").On("events")                            
                            .ListeningEvents(typeof(CashOutProcessedEvent))
                                .From("post-processing").On("events")
                                .WithEndpointResolver(sagasProtobufEndpointResolver)
                            .PublishingCommands(typeof(CompleteActivityCommand), typeof(FailActivityCommand))
                                .To("operations").With("commands"),

                        Register.Saga<BlockchainCashoutSaga>("blockchain-cashout-saga")
                            .ListeningEvents(typeof(ExternalExecutionActivityCreatedEvent)).From("operations").On("events")
                            .ListeningEvents(typeof(OperationExecutionFailedEvent)).From(BlockchainOperationsExecutorBoundedContext.Name).On("events")
                            .ListeningEvents(typeof(Bitcoin.Contracts.Events.CashoutCompletedEvent)).From(BitcoinBoundedContext.Name).On("events")
                            .ListeningEvents(typeof(Job.EthereumCore.Contracts.Cqrs.Events.CashoutCompletedEvent)).From(EthereumBoundedContext.Name).On("events")
                            .ListeningEvents(typeof(SolarCashOutCompletedEvent)).From("solarcoin").On("events")
                            .ListeningEvents(typeof(CashoutCompletedEvent)).From(BlockchainCashoutProcessorBoundedContext.Name).On("events")
                            .PublishingCommands(typeof(StartCashoutCommand)).To(BlockchainCashoutProcessorBoundedContext.Name).With("commands")
                            .PublishingCommands(typeof(Job.EthereumCore.Contracts.Cqrs.Commands.StartCashoutCommand)).To(EthereumBoundedContext.Name).With("commands")
                            .PublishingCommands(typeof(SolarCashOutCommand)).To("solarcoin").With("commands")
                            .PublishingCommands(typeof(Bitcoin.Contracts.Commands.StartCashoutCommand)).To(BitcoinBoundedContext.Name).With("commands")
                            .PublishingCommands(typeof(CompleteActivityCommand), typeof(FailActivityCommand)).To("operations").With("commands"),

                        //Register.Saga<WorkflowSaga>("workflow-saga")
                        //    .ListeningEvents(typeof(OperationCreatedEvent)).From("operations").On("events")
                        //    .PublishingCommands(typeof(ExecuteOperationCommand)).To("operations").With("commands"),

                        Register.DefaultRouting
                            .PublishingCommands(typeof(ExecuteOperationCommand), typeof(CompleteActivityCommand), typeof(FailActivityCommand))
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
