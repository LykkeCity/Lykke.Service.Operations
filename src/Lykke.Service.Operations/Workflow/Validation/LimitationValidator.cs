using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class LimitationValidator : AbstractValidator<LimitationInput>
    {
        public LimitationValidator(ILimitationsServiceClient limitationsServiceClient)
        {
            RuleFor(m => m.Volume)
                .MustAsync(async (input, volume, ctx, token) =>
                {
                    var result = await limitationsServiceClient.CheckAsync(input.ClientId, input.AssetId,
                        (double) volume,
                        input.OperationType);

                    ctx.Rule.MessageBuilder = c => result.FailMessage;

                    return result.IsValid;
                })
                .WithErrorCode("LimitationCheckFailed");
        }
    }
}
