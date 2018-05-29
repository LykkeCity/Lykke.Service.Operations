using System.Threading.Tasks;
using FluentValidation;
using JetBrains.Annotations;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Operations.Workflow.Data;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class BalanceCheckValidator : AbstractValidator<BalanceCheckInput>
    {
        private readonly IBalancesClient _balancesClient;

        public BalanceCheckValidator(IBalancesClient balancesClient)
        {
            _balancesClient = balancesClient;

            RuleFor(m => m.Volume)
                .Must(volume => volume > 0)
                .WithErrorCode("InvalidField")
                .WithMessage("Volume");

            RuleFor(m => m.Volume)
                .MustAsync(async (input, volume, token) => await GetBalance(input.AssetId, input.ClientId) >= volume)
                .WithErrorCode("NotEnoughFunds")
                .WithMessage(input => "Not enough funds");
        }

        private async Task<decimal> GetBalance(string assetId, string clientId)
        {
            var result =
                await _balancesClient.GetClientBalanceByAssetId(new ClientBalanceByAssetIdModel(assetId, clientId));

            if (result?.Balance == null)
                return 0;

            return (decimal)result.Balance;
        }
    }
}
