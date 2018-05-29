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
            When(x => x.Type == Contracts.OperationType.LimitOrder || x.Type == Contracts.OperationType.MarketOrder,
                () =>
                {
                    RuleFor(i => i.ClientId)
                        .MustAsync(async (ctx, input, token) =>
                        {
                            var result = await assetDisclaimersClient
                               .CheckTradableClientDisclaimerAsync(ctx.ClientId, ctx.LykkeEntityId1,
                                    ctx.LykkeEntityId2);

                            return !result.RequiresApproval;
                        })
                        .WithErrorCode("PendingDisclaimer")
                        .WithMessage("User has pending disclaimer");
                });

            When(x => x.Type == Contracts.OperationType.CashoutSwift,
                () =>
                {
                    RuleFor(i => i.ClientId)
                        .MustAsync(async (ctx, input, token) =>
                        {
                            var result = await assetDisclaimersClient.CheckWithdrawalClientDisclaimerAsync(ctx.ClientId, ctx.LykkeEntityId1);

                            return !result.RequiresApproval;
                        })
                        .WithErrorCode("PendingDisclaimer")
                        .WithMessage("User has pending disclaimer");
                });
        }
    }
}
