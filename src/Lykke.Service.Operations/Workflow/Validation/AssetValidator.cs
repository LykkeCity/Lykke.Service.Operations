using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class AssetValidator : AbstractValidator<AssetInput>
    {
        public AssetValidator()
        {
            RuleFor(m => m.IsTradable)
                .Equal(true)
                .WithMessage(asset => $"Asset '{asset.DisplayId}' must be tradable");

            RuleFor(m => m.IsTrusted)
                .Equal(true)
                .WithMessage(asset => $"Asset '{asset.DisplayId}' must be trusted");
        }
    }
}
