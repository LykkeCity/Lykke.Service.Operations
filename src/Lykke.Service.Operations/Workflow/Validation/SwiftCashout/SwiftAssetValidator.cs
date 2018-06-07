using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Operations.Workflow.Data.SwiftCashout;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class SwiftAssetValidator : AbstractValidator<AssetInput>
    {
        public SwiftAssetValidator()
        {
            RuleFor(m => m.SwiftCashoutEnabled)
                .Equal(true)
                .WithErrorCode("AssetSwiftDisabled")
                .WithMessage(asset => $"Swift cashout disabled");
        }
    }
}
