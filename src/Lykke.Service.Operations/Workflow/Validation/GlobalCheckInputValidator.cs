using FluentValidation;

namespace Lykke.Service.Operations.Workflow.Validation
{
    internal class GlobalCheckInputValidator : AbstractValidator<GlobalCheckInput>
    {
        public GlobalCheckInputValidator()
        {
            RuleFor(t => t.CashOutBlocked)
                .Equal(false)
                .WithMessage("Cashout is globally locked");
        }
    }
}
