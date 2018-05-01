using FluentValidation;
using JetBrains.Annotations;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    internal class GlobalValidator : AbstractValidator<GlobalInput>
    {
        public GlobalValidator()
        {
            RuleFor(t => t.CashOutBlocked)
                .Equal(false)
                .WithMessage("Cashout is globally locked");
        }
    }
}
