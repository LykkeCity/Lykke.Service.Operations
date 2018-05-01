using System.Linq;
using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.RateCalculator.Client.AutorestClient.Models;
using OrderAction = Lykke.Service.Operations.Contracts.OrderAction;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class AssetValidator : AbstractValidator<AssetInput>
    {
        public AssetValidator()
        {
            RuleFor(m => m.IsTradable)
                .Equal(true)
                .WithMessage(asset => $"Asset '{asset.Id}' must be tradable");

            RuleFor(m => m.IsTrusted)
                .Equal(true)
                .WithMessage(asset => $"Asset '{asset.Id}' must be trusted");
        }
    }
}
