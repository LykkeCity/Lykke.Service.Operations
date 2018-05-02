using FluentValidation;
using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Models
{
    public class CreatePaymentCommandValidator: AbstractValidator<CreatePaymentCommand>
    {
        public CreatePaymentCommandValidator()
        {
            RuleFor(m => m.AssetId)
                .NotEmpty();

            RuleFor(m => m.Amount)
                .GreaterThan(0);
        }
    }
}
