using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Frontend.WampHost.Contracts;
using Lykke.Frontend.WampHost.Contracts.Commands;
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
using Lykke.Service.ConfirmationCodes.Contract;
using Lykke.Service.ConfirmationCodes.Contract.Commands;
using Lykke.Service.ConfirmationCodes.Contract.Events;
using Lykke.Service.Operations.Contracts;
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
            MessagePackSerializerFactory.Defaults.FormatterResolver = MessagePack.Resolvers.ContractlessStandardResolver.Instance;

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
            builder.RegisterType<ConfirmationSaga>().SingleInstance();

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

                        Register.BoundedContext(OperationsBoundedContext.Name)
                            .ListeningCommands(
                                typeof(CreateCashoutCommand),
                                typeof(ConfirmCommand),
                                typeof(CreateSwiftCashoutCommand))
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
                                typeof(OperationConfirmedEvent),
                                typeof(OperationCompletedEvent),
                                typeof(ExternalExecutionActivityCreatedEvent),
                                typeof(ConfirmationReceivedEvent))
                                .With("events"),

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
                                .From(OperationsBoundedContext.Name).On("events")
                            .ListeningEvents(typeof(CashOutProcessedEvent))
                                .From("post-processing").On("events")
                                .WithEndpointResolver(sagasProtobufEndpointResolver)
                            .PublishingCommands(typeof(CompleteActivityCommand), typeof(FailActivityCommand))
                                .To(OperationsBoundedContext.Name).With("commands"),

                        Register.Saga<BlockchainCashoutSaga>("blockchain-cashout-saga")
                            .ListeningEvents(typeof(ExternalExecutionActivityCreatedEvent)).From(OperationsBoundedContext.Name).On("events")
                            .ListeningEvents(typeof(Job.EthereumCore.Contracts.Cqrs.Events.CashoutCompletedEvent)).From(EthereumBoundedContext.Name).On("events")
                            .ListeningEvents(typeof(SolarCashOutCompletedEvent)).From("solarcoin").On("events")
                            .ListeningEvents(
                                typeof(CashoutCompletedEvent), 
                                typeof(CashoutsBatchCompletedEvent),
                                typeof(CrossClientCashoutCompletedEvent),
                                typeof(CashoutFailedEvent),
                                typeof(CashoutsBatchFailedEvent))
                            .From(BlockchainCashoutProcessorBoundedContext.Name).On("events")
                            .PublishingCommands(typeof(StartCashoutCommand)).To(BlockchainCashoutProcessorBoundedContext.Name).With("commands")
                            .PublishingCommands(typeof(Job.EthereumCore.Contracts.Cqrs.Commands.StartCashoutCommand)).To(EthereumBoundedContext.Name).With("commands")
                            .PublishingCommands(typeof(SolarCashOutCommand)).To("solarcoin").With("commands")                            
                            .PublishingCommands(typeof(CompleteActivityCommand), typeof(FailActivityCommand)).To(OperationsBoundedContext.Name).With("commands"),

                        Register.Saga<WorkflowSaga>("workflow-saga")
                            .ListeningEvents(typeof(OperationCreatedEvent))
                                .From(OperationsBoundedContext.Name).On("events")
                            .PublishingCommands(typeof(ExecuteOperationCommand))
                                .To(OperationsBoundedContext.Name).With("commands"),

                        Register.Saga<ConfirmationSaga>("confirmation-saga")
                            .ListeningEvents(typeof(ExternalExecutionActivityCreatedEvent), typeof(ConfirmationReceivedEvent))
                                .From(OperationsBoundedContext.Name).On("events")
                            .ListeningEvents(typeof(ConfirmationValidationPassedEvent), typeof(ConfirmationValidationFailedEvent))
                                .From(ConfirmationCodesBoundedContext.Name).On("events")
                            .PublishingCommands(typeof(RequestConfirmationCommand))
                                .To(WampHostBoundedContext.Name).With("commands")
                                .WithEndpointResolver(sagasProtobufEndpointResolver)
                            .PublishingCommands(typeof(CompleteActivityCommand))
                                .To(OperationsBoundedContext.Name).With("commands")
                            .PublishingCommands(typeof(ValidateConfirmationCommand))
                                .To(ConfirmationCodesBoundedContext.Name).With("commands"),

                        Register.DefaultRouting
                            .PublishingCommands(typeof(ExecuteOperationCommand), typeof(CompleteActivityCommand), typeof(FailActivityCommand))
                                .To(OperationsBoundedContext.Name).With("commands")
                    );
                })
                .As<ICqrsEngine>()
                .SingleInstance();
        }
    }
}
