using System;
using System.Threading.Tasks;
using Common;
using JetBrains.Annotations;
using Lykke.Bitcoin.Contracts;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;
using Lykke.Job.EthereumCore.Contracts.Cqrs;
using Lykke.Service.Operations.Services;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Events;
using Newtonsoft.Json;

namespace Lykke.Service.Operations.Workflow.Sagas
{
    public class BlockchainCashoutSaga
    {
        [UsedImplicitly]
        public async Task Handle(ExternalExecutionActivityCreatedEvent evt, ICommandSender commandSender)
        {
            if (evt.Type == "SettleOnBlockchain")
            {
                var input = JsonConvert.DeserializeObject<BlockchainCashoutInput>(evt.Input);

                if (!string.IsNullOrWhiteSpace(input.BlockchainIntegrationLayerId))
                {
                    var command = new StartCashoutCommand
                    {
                        OperationId = input.OperationId,
                        AssetId = input.AssetId,
                        Amount = input.Amount,
                        ToAddress = input.ToAddress,
                        ClientId = input.ClientId
                    };

                    commandSender.SendCommand(command, BlockchainCashoutProcessorBoundedContext.Name);
                }
                else if (input.AssetBlockchain == "Ethereum")
                {
                    var command = new Job.EthereumCore.Contracts.Cqrs.Commands.StartCashoutCommand
                    {
                        Id = evt.Id,
                        AssetId = input.AssetId,
                        Amount = input.Amount,
                        FromAddress = input.EthHotWallet,
                        ToAddress = input.ToAddress
                    };

                    commandSender.SendCommand(command, EthereumBoundedContext.Name);
                }
                else if (input.AssetId == "SLR")
                {
                    var command = new SolarCashOutCommand
                    {
                        Id = evt.Id.ToString(),
                        ClientId = input.ClientId.ToString(),
                        Address = input.ToAddress,
                        Amount = input.Amount
                    };

                    commandSender.SendCommand(command, "Solarcoin");
                }                
                else if (input.AssetBlockchain == "Bitcoin" && input.AssetBlockchainWithdrawal)
                {
                    var commnad = new Bitcoin.Contracts.Commands.StartCashoutCommand
                    {
                        Id = evt.Id,
                        AssetId = input.AssetId,
                        Amount = input.Amount,
                        Address = input.ToAddress
                    };

                    commandSender.SendCommand(commnad, BitcoinBoundedContext.Name);
                }
            }
        }        

        [UsedImplicitly]
        public async Task Handle(Bitcoin.Contracts.Events.CashoutCompletedEvent evt, ICommandSender commandSender)
        {
            var command = new CompleteActivityCommand
            {
                OperationId = evt.OperationId,
                Output = "{}"
            };

            commandSender.SendCommand(command, "operations");
        }

        [UsedImplicitly]
        public async Task Handle(Job.EthereumCore.Contracts.Cqrs.Events.CashoutCompletedEvent evt, ICommandSender commandSender)
        {
            var command = new CompleteActivityCommand
            {
                OperationId = evt.OperationId,
                Output = "{}"
            };

            commandSender.SendCommand(command, "operations");
        }

        [UsedImplicitly]
        public async Task Handle(SolarCashOutCompletedEvent evt, ICommandSender commandSender)
        {
            var command = new CompleteActivityCommand
            {
                OperationId = new Guid(evt.OperationId),
                Output = "{}"
            };

            commandSender.SendCommand(command, "operations");
        }
        
        [UsedImplicitly]
        public async Task Handle(CashoutCompletedEvent evt, ICommandSender commandSender)
        {
            var command = new CompleteActivityCommand
            {
                OperationId = evt.OperationId,
                Output = new
                {
                    evt.TransactionHash                    
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }

        public async Task Handle(OperationExecutionFailedEvent evt, ICommandSender commandSender)
        {
            var command = new FailActivityCommand
            {
                OperationId = evt.OperationId,
                Output = new
                {
                    ErrorCode = evt.ErrorCode.ToString(),
                    ErrorMessage = evt.Error
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }
    }
}
