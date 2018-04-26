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
            
            When(m => m.OrderAction == OrderAction.Buy, () =>
            {
                When(m => m.NeededConversionResult != null && m.NeededConversionResult.Length > 0, () =>
                {
                    RuleFor(m => m.NeededConversionResult)
                        .Cascade(CascadeMode.StopOnFirstFailure)
                        .Must(m => m.Any(x => x == OperationResult.Ok))
                        .WithMessage("There is not enough liquidity in the order book. Please try to send smaller order.");
                });

                RuleFor(m => m.NeededAmount)                    
                    .Must((input, neededAmount) => input.WalletBalance >= (double) neededAmount)
                    .WithMessage(asset => $"{asset.NeededAssetId}:  Not enough funds");
            });
        }
    }
}
