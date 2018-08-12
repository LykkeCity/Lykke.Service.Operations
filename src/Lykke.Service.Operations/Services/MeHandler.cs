using System;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.Operations.Modules;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;

namespace Lykke.Service.Operations.Services
{
    public class MeHandler
    {
        private readonly IMatchingEngineClient _matchingEngineClient;
        
        public MeHandler(IMatchingEngineClient matchingEngineClient)
        {
            _matchingEngineClient = matchingEngineClient;            
        }

        public async Task<CommandHandlingResult> Handle(MeCashoutCommand cmd, IEventPublisher eventPublisher)
        {
            var result = await _matchingEngineClient.CashInOutAsync(
                cmd.OperationId.ToString(),
                cmd.RequestId.ToString(),
                cmd.ClientId,
                cmd.AssetId,
                cmd.AssetAccuracy,
                (double)-Math.Abs(cmd.Amount),
                cmd.FeeClientId,
                cmd.FeeSize,
                cmd.FeeType == FeeType.Absolute
                    ? MatchingEngine.Connector.Models.Common.FeeSizeType.ABSOLUTE
                    : MatchingEngine.Connector.Models.Common.FeeSizeType.PERCENTAGE);
            
            if (result == null)
            {
                eventPublisher.PublishEvent(new MeCashoutFailedEvent
                {
                    OperationId = cmd.OperationId,
                    RequestId = cmd.RequestId,
                    ErrorMessage = "ME is not available"
                });                
            }
            else if (result.Status != MeStatusCodes.Ok)
            {
                eventPublisher.PublishEvent(new MeCashoutFailedEvent
                {
                    OperationId = cmd.OperationId,
                    RequestId = cmd.RequestId,
                    ErrorCode = result.Status.ToString(),
                    ErrorMessage = result.Message
                });
            }

            return CommandHandlingResult.Ok();
        }
    }

    public class MeCashoutFailedEvent
    {
        public Guid OperationId { get; set; }
        public Guid RequestId { get; set; }

        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }        
    }    
}
