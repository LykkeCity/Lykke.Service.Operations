using FluentValidation;
using Lykke.Service.RateCalculator.Client.AutorestClient.Models;

namespace Lykke.Service.Operations.Workflow.Validation
{
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
                When(m => m.NeededConversionResult.HasValue, () =>
                {
                    RuleFor(m => m.NeededConversionResult)
                        .Equal(OperationResult.Ok)
                        .WithMessage("There is not enough liquidity in the order book. Please try to send smaller order.");
                });

                RuleFor(m => m.NeededAmount)                    
                    .Must((input, neededAmount) => input.WalletBalance >= (double) neededAmount)
                    .WithMessage(asset => $"{asset.NeededAssetId}:  Not enough funds");
            });
        }
    }
}
