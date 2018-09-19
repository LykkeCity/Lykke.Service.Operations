using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Frontend.WampHost.Contracts;
using Lykke.Frontend.WampHost.Contracts.Commands;
using Lykke.Service.ConfirmationCodes.Contract;
using Lykke.Service.ConfirmationCodes.Contract.Commands;
using Lykke.Service.ConfirmationCodes.Contract.Events;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Workflow;
using Newtonsoft.Json;

namespace Lykke.Service.Operations.Workflow.Sagas
{
    public class ConfirmationSaga
    {
        private readonly ILog _log;
        private readonly IOperationsRepository _operationsRepository;

        public ConfirmationSaga(ILogFactory logFactory, IOperationsRepository operationsRepository)
        {
            _operationsRepository = operationsRepository;
            _log = logFactory.CreateLog(this);
        }

        [UsedImplicitly]
        public async Task Handle(ExternalExecutionActivityCreatedEvent evt, ICommandSender commandSender)
        {
            if (evt.Type == "RequestConfirmation")
            {
                var input = JsonConvert.DeserializeObject<ConfirmationRequestInput>(evt.Input);

                commandSender.SendCommand(new RequestConfirmationCommand
                {
                    ClientId = input.ClientId,
                    OperationId = input.OperationId,
                    ConfirmationType = input.ConfirmationType
                }, WampHostBoundedContext.Name);
            }
            else if (evt.Type == "ValidateConfirmation")
            {
                var input = JsonConvert.DeserializeObject<ValidateConfirmationInput>(evt.Input);

                commandSender.SendCommand(new ValidateConfirmationCommand
                {
                    Id = input.OperationId.ToString(),
                    ClientId = input.ClientId,
                    Confirmation = input.Confirmation,
                    Type = input.ConfirmationType
                }, ConfirmationCodesBoundedContext.Name);
            }
        }

        [UsedImplicitly]
        public async Task Handle(ConfirmationReceivedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"ConfirmationValidationPassedEvent for operation [{evt.OperationId}] received", evt);

            var operation = await _operationsRepository.Get(evt.OperationId);

            var hasPendingConfirmationActivity = GetConfirmationActivity(operation) != null;

            if (hasPendingConfirmationActivity)
            {
                var command = new CompleteActivityCommand
                {
                    OperationId = evt.OperationId,
                    Output = new
                    {
                        Confirmation = new
                        {
                            Code = evt.Confirmation
                        }
                    }.ToJson()
                };

                commandSender.SendCommand(command, "operations");
            }
        }

        [UsedImplicitly]
        public async Task Handle(ConfirmationValidationPassedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"ConfirmationValidationPassedEvent for operation [{evt.Id}] received", evt);

            var command = new CompleteActivityCommand
            {
                OperationId = Guid.Parse(evt.Id),
                Output = new
                {
                    Confirmation = new
                    {
                        IsValid = true
                    }
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }

        [UsedImplicitly]
        public async Task Handle(ConfirmationValidationFailedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"ConfirmationValidationFailedEvent for operation [{evt.Id}] received", evt);

            var command = new CompleteActivityCommand
            {
                OperationId = Guid.Parse(evt.Id),
                Output = new
                {
                    Confirmation = new
                    {
                        IsValid = false
                    }
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }

        private OperationActivity GetConfirmationActivity(Operation operation)
        {
            return operation?.Activities.LastOrDefault(a => a.Type == "RequestConfirmation" && a.Status == ActivityResult.None);
        }
    }
}
