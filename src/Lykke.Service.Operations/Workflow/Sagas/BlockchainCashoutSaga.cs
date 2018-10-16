﻿using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Job.BlockchainCashoutProcessor.Contract;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Commands;
using Lykke.Job.BlockchainCashoutProcessor.Contract.Events;
using Lykke.Job.BlockchainOperationsExecutor.Contract.Events;
using Lykke.Job.EthereumCore.Contracts.Cqrs;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Events;
using Newtonsoft.Json;

namespace Lykke.Service.Operations.Workflow.Sagas
{
    public class BlockchainCashoutSaga
    {
        private readonly ILog _log;

        public BlockchainCashoutSaga(ILogFactory logFactory)
        {
            _log = logFactory.CreateLog(this);
        }

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

                    _log.Info($"StartCashoutCommand for BIL has sent. Operation [{command.OperationId}]", command);
                }
                else if (input.AssetBlockchain == "Ethereum")
                {
                    var command = new Job.EthereumCore.Contracts.Cqrs.Commands.StartCashoutCommand
                    {
                        Id = input.OperationId,
                        AssetId = input.AssetId,
                        Amount = input.Amount,
                        FromAddress = input.EthHotWallet,
                        ToAddress = input.ToAddress
                    };

                    commandSender.SendCommand(command, EthereumBoundedContext.Name);

                    _log.Info($"StartCashoutCommand for Ethereum has sent. Operation [{command.Id}]", command);
                }
                else if (input.AssetId == "SLR")
                {
                    var command = new SolarCashOutCommand
                    {
                        Id = input.OperationId.ToString(),
                        ClientId = input.ClientId.ToString(),
                        Address = input.ToAddress,
                        Amount = input.Amount
                    };

                    commandSender.SendCommand(command, "solarcoin");

                    _log.Info($"StartCashoutCommand for Solarcoin has sent. Operation [{command.Id}]", command);
                }                                
            }
        }              

        [UsedImplicitly]
        public async Task Handle(Job.EthereumCore.Contracts.Cqrs.Events.CashoutCompletedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"CashoutCompletedEvent for Ethereum received. Operation [{evt.OperationId}]", evt);

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
            _log.Info($"CashoutCompletedEvent for Solarcoin received. Operation [{evt.OperationId}]", evt);

            var command = new CompleteActivityCommand
            {
                OperationId = new Guid(evt.OperationId),
                Output = "{}"
            };

            commandSender.SendCommand(command, "operations");
        }

        #region CashourProcessorEvents

        [UsedImplicitly]
        public async Task Handle(CashoutsBatchCompletedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"CashoutsBatchCompletedEvent for BIL received. BatchId [{evt.BatchId}]", evt);

            if (evt.Cashouts == null || evt.Cashouts.Length == 0)
            {
                _log.Warning($"Empty cashouts in batch. BatchId [{evt.BatchId}]", context: evt);

                return;
            }

            foreach (var cashout in evt.Cashouts)
            {
                var command = new CompleteActivityCommand
                {
                    OperationId = cashout.OperationId,
                    Output = new
                    {
                        evt.TransactionHash
                    }.ToJson()
                };

                commandSender.SendCommand(command, "operations");
            }
        }

        [UsedImplicitly]
        public async Task Handle(CashoutCompletedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"CashoutCompletedEvent for BIL received. Operation [{evt.OperationId}]", evt);

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

        [UsedImplicitly]
        public async Task Handle(CrossClientCashoutCompletedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"CrossClientCashoutCompletedEvent for BIL received. Operation [{evt.OperationId}]", evt);

            var command = new CompleteActivityCommand
            {
                OperationId = evt.OperationId,
            };

            commandSender.SendCommand(command, "operations");
        }

        [UsedImplicitly]
        public async Task Handle(CashoutFailedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"OperationExecutionFailedEvent for BIL received. Operation [{evt.OperationId}]", evt);

            var command = new FailActivityCommand
            {
                OperationId = evt.OperationId,
                Output = new
                {
                    ErrorCode = "BIL error",
                    ErrorMessage = evt.Error
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }

        [UsedImplicitly]
        public async Task Handle(CashoutsBatchFailedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"CashoutsBatchFailedEvent for BIL received. BatchId [{evt.BatchId}]", evt);

            if (evt.Cashouts == null || evt.Cashouts.Length == 0)
            {
                _log.Warning($"Empty cashouts in batch. BatchId [{evt.BatchId}]", context:evt);

                return;
            }

            foreach (var cashout in evt.Cashouts)
            {
                var command = new FailActivityCommand
                {
                    OperationId = cashout.OperationId,
                    Output = new
                    {
                        ErrorCode = "BIL error",
                        ErrorMessage = evt.Error
                    }.ToJson()
                };

                commandSender.SendCommand(command, "operations");
            }
        }

        #endregion

        [Obsolete]
        [UsedImplicitly]
        public async Task Handle(OperationExecutionFailedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"OperationExecutionFailedEvent for BIL received. Operation [{evt.OperationId}]", evt);

            var command = new FailActivityCommand
            {
                OperationId = evt.OperationId,
                Output = new
                {
                    ErrorCode = "BIL error",
                    ErrorMessage = evt.Error
                }.ToJson()
            };

            commandSender.SendCommand(command, "operations");
        }
    }
}
