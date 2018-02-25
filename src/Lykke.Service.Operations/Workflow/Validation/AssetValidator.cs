using FluentValidation;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class AssetValidator : AbstractValidator<AssetInput>
    {
        public AssetValidator()
        {
            RuleFor(m => m.IsTradable)
                .Equal(true)
                .WithMessage("Asset must be tradable");

            RuleFor(m => m.IsTrusted)
                .Equal(true)
                .WithMessage("Asset must be trusted");

            RuleFor(m => m.NeededAssetIsTrusted)
                .Equal(true)
                .WithMessage("Needed asset is not trusted");

            When(m => m.OrderAction == OrderAction.Buy, () =>
            {
                RuleFor(m => m.NeededAmount)
                    .Must((input, neededAmount) => input.WalletBalance >= (double) neededAmount)
                    .WithMessage("Not enough funds");
            });
        }
    }
}
