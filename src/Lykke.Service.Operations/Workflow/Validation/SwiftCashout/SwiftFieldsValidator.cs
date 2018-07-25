using Common;
using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Operations.Workflow.Data;
using System;
using System.Text.RegularExpressions;

namespace Lykke.Service.Operations.Workflow.Validation.SwiftCashout
{
    [UsedImplicitly]
    public class SwiftFieldsValidator : AbstractValidator<SwiftInput>
    {
        private readonly Regex _swiftRegex = new Regex("^[a-zA-Z]{6}[a-zA-Z0-9]{2}([a-zA-Z0-9]{3})?$");

        public SwiftFieldsValidator()
        {
            RuleFor(m => m.Bic)
                .Must(bic =>
                {
                    var code = bic.GetCountryCode();
                    return code != null && _swiftRegex.IsMatch(bic) && CountryManager.HasIso2(code);
                })
                .WithErrorCode("InvalidField")
                .WithMessage("Bic");

            RuleFor(m => m.AccHolderAddress)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccHolderAddress");

            RuleFor(m => m.AccHolderCity)
                .NotEmpty()
                .WithErrorCode("InvalidField")
                .WithMessage("AccHolderCity");

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
        }
    }
}
