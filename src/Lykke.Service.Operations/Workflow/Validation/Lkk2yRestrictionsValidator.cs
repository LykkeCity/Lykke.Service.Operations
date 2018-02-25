using FluentValidation;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Workflow.Extensions;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class Lkk2yRestrictionsValidator : AbstractValidator<Lkk2yRestrictionsInput>
    {
        public Lkk2yRestrictionsValidator()
        {
            RuleFor(m => m.CountryFromPOA)
                .Must((input, country) => !IsLkk2YRestrictedForUser(input.CountryFromPOA, input.IcoSettings, input.BaseAssetId, input.QuotingAssetId))
                .WithMessage("Sorry, LKK2Y operations are restricted in your region");
        }

        private static bool IsLkk2YRestrictedForUser(string countryFromPoa, IcoSettings icoSettings, string baseAssetId, string quotingAssetId)
        {
            return (baseAssetId == icoSettings.LKK2YAssetId ||
                    quotingAssetId == icoSettings.LKK2YAssetId) &&
                   icoSettings.IsRestricted(countryFromPoa);
        }
    }
}
