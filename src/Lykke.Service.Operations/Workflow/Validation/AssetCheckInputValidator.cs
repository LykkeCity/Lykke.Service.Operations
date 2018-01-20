using FluentValidation;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class AssetCheckInputValidator : AbstractValidator<AssetCheckInput>
    {
        public AssetCheckInputValidator()
        {
            RuleFor(m => m.IsTradable)
                .Equal(true)
                .WithMessage("Asset must be tradable");

            RuleFor(m => m.IsTrusted)
                .Equal(true)
                .WithMessage("Asset must be trusted");
        }
    }
}
