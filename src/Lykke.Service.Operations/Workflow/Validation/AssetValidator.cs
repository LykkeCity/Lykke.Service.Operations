using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class AssetValidator : AbstractValidator<AssetInput>
    {
        public AssetValidator()
        {
            RuleFor(m => m.Id)                
                .Must((input, id) => input.IsTradable)
                .WithName("AssetId")
                .WithErrorCode("InvalidInputField")
                .WithMessage(asset => $"Asset '{asset.DisplayId}' must be tradable");

            RuleFor(m => m.Id)
                .Must((input, id) => input.IsTrusted)
                .WithName("AssetId")
                .WithErrorCode("InvalidInputField")
                .WithMessage(asset => $"Asset '{asset.DisplayId}' must be trusted");
        }
    }
}
