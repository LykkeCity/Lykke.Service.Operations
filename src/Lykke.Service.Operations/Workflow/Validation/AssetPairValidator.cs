﻿using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Operations.Workflow.Extensions;

namespace Lykke.Service.Operations.Workflow.Validation
{
    public class AssetPairValidator : AbstractValidator<AssetPairInput>
    {
        public AssetPairValidator()
        {
            RuleFor(m => m.Id)
                .Must((input, id) => !input.BlockedAssetPairs.Contains(id))
                .WithMessage(id => $"Operation for the asset pair '{id}' is disabled.");

            RuleFor(m => m.BaseAssetId)
                .Must((input, id) => !IsOperationForAssetDisabled(id, input.BaseAssetBlockain, input.BitcoinBlockchainOperationsDisabled, input.BtcOperationsDisabled))
                .WithMessage(id => $"Operation for the asset '{id}' is disabled.");

            When(input => input.AssetId == input.BaseAssetId, () =>
            {
                RuleFor(m => m.BaseAssetId)
                    .Must((input, id) => input.Volume >= input.MinVolume)
                    .WithMessage(input => $"Asset {input.BaseAssetId}. Volume '{input.Volume}' must be greater than minimum volume '{input.MinVolume}'");
            });

            RuleFor(m => m.QuotingAssetId)
                .Must((input, id) => !IsOperationForAssetDisabled(id, input.QuotingAssetBlockchain, input.BitcoinBlockchainOperationsDisabled, input.BtcOperationsDisabled))
                .WithMessage(id => $"Operation for the asset '{id}' is disabled.");

            When(input => input.AssetId == input.QuotingAssetId, () =>
            {
                RuleFor(m => m.QuotingAssetId)
                    .Must((input, id) => input.Volume >= input.MinInvertedVolume)
                    .WithMessage(input => $"Asset {input.QuotingAssetId}. Volume '{input.Volume}' must be greater than minimum inverted volume '{input.MinInvertedVolume}'");
            });
        }

        private bool IsOperationForAssetDisabled(string assetId, Blockchain assetBlockchain, bool btcBlockchainOpsDisabled, bool btcOnlyDisabled)
        {                        
            return btcOnlyDisabled && assetId == LykkeConstants.BitcoinAssetId || btcBlockchainOpsDisabled && assetBlockchain == Blockchain.Bitcoin;
        }        
    }
}
