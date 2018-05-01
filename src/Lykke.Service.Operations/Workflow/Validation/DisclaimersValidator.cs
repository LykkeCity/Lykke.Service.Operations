using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class DisclaimersValidator : AbstractValidator<DisclaimerInput>
    {        
        public DisclaimersValidator(IAssetDisclaimersClient assetDisclaimersClient)
        {
            RuleFor(i => i.ClientId)
                .Must((ctx, input) =>
                {
                    return !assetDisclaimersClient
                            .CheckTradableClientDisclaimerAsync(ctx.ClientId, ctx.LykkeEntityId1, ctx.LykkeEntityId2)
                            .GetAwaiter().GetResult().RequiresApproval;
                })
                .WithMessage("User has pending disclaimer");
        }
    }
}
