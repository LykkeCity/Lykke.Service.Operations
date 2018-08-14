using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Service.Operations.Modules;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Events;
using Newtonsoft.Json;

namespace Lykke.Service.Operations.Workflow.Sagas
{
    public class MeSaga
    {
        [UsedImplicitly]
        public async Task Handle(ExternalExecutionActivityCreatedEvent evt, ICommandSender commandSender)
        {
            if (evt.Type == "WaitForResultsFromME")
            {
                // NOP. Step for waiting ME results.
            }            
        }

        [UsedImplicitly]
        public async Task Handle(CashOutProcessedEvent evt, ICommandSender commandSender)
        {
            var command = new CompleteActivityCommand
            {
                OperationId = evt.OperationId,                
                Output = new
                {
                    evt.AssetId,
                    evt.WalletId,
                    evt.Volume,
                    evt.Timestamp,
                    evt.FeeSize                    
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }

        [UsedImplicitly]
        public async Task Handle(MeCashoutFailedEvent evt, ICommandSender commandSender)
        {
            var command = new FailActivityCommand
            {
                OperationId = evt.OperationId,
                ActivityId = evt.RequestId,
                Output = new
                {
                    evt.ErrorCode,
                    evt.ErrorMessage
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }
    }
}
