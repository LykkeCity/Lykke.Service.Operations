using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Service.Assets.Client;
using Lykke.Service.Operations.Workflow.Commands;
using Lykke.Service.Operations.Workflow.Events;
using Lykke.Service.PostProcessing.Contracts.Cqrs.Events;

namespace Lykke.Service.Operations.Workflow.Sagas
{
    public class MeSaga
    {
        private readonly ILog _log;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;

        public MeSaga(ILogFactory log, IAssetsServiceWithCache assetsServiceWithCache)
        {
            _log = log.CreateLog(this);
            _assetsServiceWithCache = assetsServiceWithCache;
        }

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
            _log.Info($"CashOutProcessedEvent for operation [{evt.OperationId}] received", evt);

            var asset = await _assetsServiceWithCache.TryGetAssetAsync(evt.AssetId);

            if (asset.SwiftWithdrawal || asset.ForwardWithdrawal)
            {
                _log.Info($"CashOutProcessedEvent for operation [{evt.OperationId}] skipped (swift or forward)", evt);

                return;
            }

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
                }.ToJson(),
                ActivityType = nameof(IActivityReference.WaitForResultsFromMe)
            };

            commandSender.SendCommand(command, "operations");
        }

        [UsedImplicitly]
        public async Task Handle(MeCashoutFailedEvent evt, ICommandSender commandSender)
        {
            _log.Info($"MeCashoutFailedEvent for operation [{evt.OperationId}] received", evt);

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
