using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.Operations.Core.Services;

namespace Lykke.Service.Operations.PeriodicalHandlers
{
    [UsedImplicitly]
    public class OperationsHandler : IStartable, IStopable
    {
        private readonly IOperationsCacheService _operationsCacheService;
        private readonly TimerTrigger _timerTrigger;
        private readonly ILog _log;

        public OperationsHandler(
            IOperationsCacheService operationsCacheService,
            ILogFactory logFactory
            )
        {
            _operationsCacheService = operationsCacheService;
            _log = logFactory.CreateLog(this);
            _timerTrigger = new TimerTrigger(nameof(OperationsHandler), TimeSpan.FromHours(1), logFactory);
            _timerTrigger.Triggered += Execute;
        }

        public void Start()
        {
            _timerTrigger.Start();
        }

        public void Stop()
        {
            _timerTrigger.Stop();
        }

        public void Dispose()
        {
            _timerTrigger.Stop();
            _timerTrigger.Dispose();
        }

        private Task Execute(ITimerTrigger timer, TimerTriggeredHandlerArgs args, CancellationToken cancellationToken)
        {
            return _operationsCacheService.ClearAsync();
        }
    }
}
