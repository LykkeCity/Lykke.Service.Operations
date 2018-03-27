using FluentValidation;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class UsaUsersValidator : AbstractValidator<UsaUsersRestrictionsInput>
    {
        public UsaUsersValidator()
        {
            RuleFor(m => m.Country)
                .Must((input, country) =>
                {
                    var otherAssetId = GetOtherAssetId(input.BaseAssetId, input.QuotingAssetId, input.AssetId);

                    bool isLkkPurchase = input.Volume > 0 && IsLkkOrLkk1YOrLkk2Y(input.AssetId) ||
                                         input.Volume < 0 && IsLkkOrLkk1YOrLkk2Y(otherAssetId);

                    return !(IsUserFromUs(country, input.CountryFromID, input.CountryFromPOA, input.KycStatus.IsKycOkOrReviewDone()) && isLkkPurchase);
                })
                .WithMessage("We are unable to accept your purchase request at this time. We will notify you when this temporary restriction has been lifted. Thank you for your understanding.");
        }

        private static bool IsUserFromUs(string country, string countryFromId, string countryFromPOA, bool kycOk)
        {
            return kycOk && (countryFromId == LykkeConstants.Iso3USA
                             || countryFromPOA == LykkeConstants.Iso3USA || countryFromId == LykkeConstants.Iso2USA) || //ToDo: remove check for ISO2 code when ISO3 will be used for storage
                   !kycOk && country == LykkeConstants.Iso3USA;
        }

        private static string GetOtherAssetId(string baseAssetId, string quotingAssetId, string assetId)
        {
            return baseAssetId == assetId ? quotingAssetId : baseAssetId;
        }

        private static bool IsLkkOrLkk1YOrLkk2Y(string assetId)
        {
            return assetId == LykkeConstants.LykkeAssetId || assetId == LykkeConstants.LykkeForwardAssetId || assetId == LykkeConstants.LykkeForward2YAssetId;
        }
    }
}
