using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation.SwiftCashout
{
    [UsedImplicitly]
    public class SwiftFieldsValidator : AbstractValidator<SwiftInput>
    {
        public SwiftFieldsValidator()
        {
            RuleFor(m => m.AccHolderAddress)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccHolderAddress");

            RuleFor(m => m.AccHolderCity)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccHolderCity");

            RuleFor(m => m.AccHolderCountry)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccHolderCountry");

            RuleFor(m => m.AccHolderZipCode)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccHolderZipCode");

            RuleFor(m => m.AccName)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccName");

            RuleFor(m => m.AccNumber)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccNumber");

            RuleFor(m => m.BankName)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("BankName");

            RuleFor(m => m.Bic)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("Bic");
        }
    }
}
