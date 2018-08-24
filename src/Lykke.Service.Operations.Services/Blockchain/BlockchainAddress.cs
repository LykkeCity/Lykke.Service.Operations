using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Contract;
using Lykke.Service.BlockchainWallets.Contract.Models;

namespace Lykke.Service.Operations.Services.Blockchain
{
    public class BlockchainAddress
    {
        private readonly AddressExtensionsCache _addressExtensionsCache;
        private readonly IBlockchainWalletsClient _blockchainWalletsClient;

        public BlockchainAddress(AddressExtensionsCache addressExtensionsCache, IBlockchainWalletsClient blockchainWalletsClient)
        {
            _addressExtensionsCache = addressExtensionsCache;
            _blockchainWalletsClient = blockchainWalletsClient;
        }

        public async Task<string> MergeAsync(string baseAddress, string addressExtension, string blockchainIntegrationLayerId)
        {            
            var (isAddressExtensionSupported,
                prohibitedCharsBase,
                prohibitedCharsExtension) = await IsAddressExtensionSupported(blockchainIntegrationLayerId);

            if (!isAddressExtensionSupported)
            {
                return baseAddress;
            }

            if (!await IsValidAddressExtension(blockchainIntegrationLayerId, addressExtension))
            {
                throw new ValidationException("Invalid address");                
            }
            try
            {
                return await MergeAddressIfNecessary(blockchainIntegrationLayerId, baseAddress, addressExtension);                
            }
            catch (ErrorResponseException e)
            {
                switch (e.Error.ErrorCode)
                {
                    case ErrorType.BaseAddressIsEmpty:
                        throw new InvalidOperationException("Base address is empty");
                    case ErrorType.BaseAddressShouldNotContainSeparator:
                        throw new InvalidOperationException($"Base address should not contain a separator symbol [{string.Join(',', prohibitedCharsBase)}]");                        
                    case ErrorType.ExtensionAddressShouldNotContainSeparator:
                        throw new InvalidOperationException($"Extension address should not contain a separator [{string.Join(',', prohibitedCharsExtension)}]");
                    default:                        
                            throw new InvalidOperationException("Exception on merge address", e);                        
                }                
            }
        }

        private async Task<string> MergeAddressIfNecessary(string blockchainIntegrationLayerId, string baseAddess, string addressExtension)
        {
            return await _blockchainWalletsClient.MergeAddressAsync
            (
                blockchainType: blockchainIntegrationLayerId,
                baseAddress: baseAddess,
                addressExtension: addressExtension
            );
        }

        private async Task<bool> IsValidAddressExtension(string blockchainIntegrationLayerId, string addressExtension)
        {
            var blockchainType = blockchainIntegrationLayerId;

            if (!string.IsNullOrEmpty(blockchainType))
            {
                var constants = await _addressExtensionsCache.GetAddressExtensionConstantsAsync(blockchainType);

                switch (constants.TypeForWithdrawal)
                {
                    case AddressExtensionTypeForWithdrawal.NotSupported:
                        return string.IsNullOrEmpty(addressExtension);
                    case AddressExtensionTypeForWithdrawal.Optional:
                        break;
                    //case AddressExtensionTypeForWithdrawal.Required:
                    //    return !string.IsNullOrEmpty(addressExtension);
                    default:
                        throw new ArgumentOutOfRangeException(constants.TypeForWithdrawal.ToString());
                }
            }

            return true;
        }

        private async Task<(bool, IEnumerable<char>, IEnumerable<char>)> IsAddressExtensionSupported(string blockchainIntegrationLayerId)
        {
            var blockchainType = blockchainIntegrationLayerId;

            if (!string.IsNullOrEmpty(blockchainType))
            {
                var constants = await _addressExtensionsCache.GetAddressExtensionConstantsAsync(blockchainType);

                return (constants.TypeForWithdrawal == AddressExtensionTypeForWithdrawal.Optional,
                    constants.ProhibitedSymbolsForBaseAddress,
                    constants.ProhibitedSymbolsForAddressExtension);
            }

            return (false, null, null);
        }
    }
}
