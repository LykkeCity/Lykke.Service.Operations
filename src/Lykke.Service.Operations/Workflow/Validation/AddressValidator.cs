using Common.Log;
using FluentValidation;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.EthereumCore.Client;
using Lykke.Service.EthereumCore.Client.Models;
using Lykke.Service.Operations.Workflow.Data;
using Lykke.Service.Operations.Workflow.Extensions;
using Nethereum.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lykke.Service.Operations.Workflow.Validation
{
    [UsedImplicitly]
    public class AddressValidator : AbstractValidator<AddressInput>
    {
        private readonly IEthereumFacade _ethereumFacade;
        private readonly SrvBlockchainValidations _srvBlockchainValidations;

        public AddressValidator(IEthereumFacade ethereumFacade, SrvBlockchainValidations srvBlockchainValidations)
        {
            _ethereumFacade = ethereumFacade;
            _srvBlockchainValidations = srvBlockchainValidations;

            When(m => string.IsNullOrWhiteSpace(m.BlockchainIntegrationLayerId), () =>
            {
                RuleFor(m => m.DestinationAddress)
                    .MustAsync(async (input, address, token) =>
                    {
                        return _ethereumFacade.IsAllowed(address).ConfigureAwait(false).GetAwaiter().GetResult();
                    })
                    .WithErrorCode("InvalidInputField")
                    .WithMessage("The destination address is not allowed for the withdrawal from the Trading wallet. Please try to send funds to your private wallet first.")
                    .When(m => m.AssetType == "Erc20Token")
                    .MustAsync(async (input, address, token) =>
                    {
                        return !string.IsNullOrWhiteSpace(input.DestinationAddress) &&
                               await IsValidCashoutAddress(input.AssetId, input.AssetBlockchain,
                                   input.DestinationAddress);
                    })
                    .WithErrorCode("InvalidInputField")
                    .WithMessage("Invalid address");
            });
        }

        private async Task<bool> IsValidCashoutAddress(string assetId, string assetBlockchain, string destinationAddress)
        {
            if (assetId == LykkeConstants.SolarAssetId)
                return SolarCoinAddress.IsValid(destinationAddress);

            if (assetId == LykkeConstants.ChronoBankAssetId)
            {
                return _ethereumFacade.IsValidAddressWithHexPrefix(destinationAddress);
            }

            if (_ethereumFacade.IsValidAddress(destinationAddress) && (assetId == LykkeConstants.ChronoBankAssetId || assetBlockchain == "Ethereum"))
                return true;

            if (assetBlockchain == "Bitcoin")
            {
                var result = await _srvBlockchainValidations.IsValidAddressToCashout(LykkeConstants.BitcoinAssetId != assetId, destinationAddress);

                if (result == ValidationErrors.None)
                    return true;
            }

            return false;
        }
    }

    public interface ISrvBlockchainReader
    {
        Task<bool> IsColoredAddress(string address);

        Task<bool> IsValidAddress(string address, bool enableColored = false);

        /// <summary>
        /// Returns uncolored address (in case when uncolored address was passed just returns it)
        /// </summary>
        /// <param name="address">Colored or uncolored address</param>
        /// <returns></returns>
        Task<string> GetUncoloredAdress(string address);
    }

    public class SrvNinjaBlockChainReader : ISrvBlockchainReader
    {
        private readonly Regex _addressRegex = new Regex("^[A-Za-z0-9]*$");
        private readonly string _url;

        public SrvNinjaBlockChainReader(string url)
        {
            if (string.IsNullOrEmpty(url))
                _url = "/";
            else
                _url = url[url.Length - 1] == '/' ? url : url + '/';
        }

        public async Task<bool> IsColoredAddress(string address)
        {
            var result = await DoRequest<WhatIsItContract>(_url + "whatisit/" + address);
            return result.Type == WhatIsItTypes.COLORED_ADDRESS;
        }

        public async Task<bool> IsValidAddress(string address, bool enableColored = false)
        {
            try
            {
                if (!_addressRegex.IsMatch(address))
                    return false;

                var result = await DoRequest<WhatIsItContract>(_url + "whatisit/" + address);

                bool valid = false;
                if (result != null)
                {
                    valid = result.Type == WhatIsItTypes.PUBKEY_ADDRESS || result.Type == WhatIsItTypes.WITNESS_SCRIPT ||
                            (result.Type == WhatIsItTypes.SCRIPT_ADDRESS && result.IsP2SH);
                    valid = enableColored ? valid || result.Type == WhatIsItTypes.COLORED_ADDRESS : valid;
                }

                return valid;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> GetUncoloredAdress(string address)
        {
            var result = await DoRequest<WhatIsItContract>(_url + "whatisit/" + address);

            if (result.Type == WhatIsItTypes.COLORED_ADDRESS)
                return result.UncoloredAddress;

            return address;
        }

        private async Task<T> DoRequest<T>(string url)
        {
            var result = await DoRequest(url);
            return JsonConvert.DeserializeObject<T>(result);
        }

        private static async Task<string> DoRequest(string url)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            using (var webResponse = await webRequest.GetResponseAsync())
            using (var receiveStream = webResponse.GetResponseStream())
            {
                using (var sr = new StreamReader(receiveStream))
                {
                    return await sr.ReadToEndAsync();
                }

            }
        }
    }

    public static class WhatIsItTypes
    {
        public const string PUBKEY_ADDRESS = "PUBKEY_ADDRESS";
        public const string SCRIPT_ADDRESS = "SCRIPT_ADDRESS";        
        public const string COLORED_ADDRESS = "COLORED_ADDRESS";        
        public const string WITNESS_SCRIPT = "WITNESS_SCRIPT_ADDRESS";        
    }

    public class WhatIsItContract
    {
        [JsonProperty("isP2SH")]
        public bool IsP2SH { get; set; }

        [JsonProperty("coloredAddress")]
        public string ColoredAddress { get; set; }

        [JsonProperty("uncoloredAddress")]
        public string UncoloredAddress { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class SrvBlockchainValidations
    {
        private readonly ISrvBlockchainReader _srvBlockchainReader;
        
        public SrvBlockchainValidations(ISrvBlockchainReader srvBlockchainReader)
        {
            _srvBlockchainReader = srvBlockchainReader;            
        }

        public async Task<ValidationErrors> IsValidAddressToCashout(bool isColoredCashOut, string destinationAddress)
        {
            if (!await _srvBlockchainReader.IsValidAddress(destinationAddress, enableColored: true))
                return ValidationErrors.InvalidAddress;

            if (isColoredCashOut && !await _srvBlockchainReader.IsColoredAddress(destinationAddress))
                return ValidationErrors.ColoredAddressExpected;
            
            return ValidationErrors.None;
        }
    }

    public enum ValidationErrors
    {
        None,
        InvalidAddress,
        ColoredAddressExpected       
    }

    public interface IEthereumFacade
    {       
        bool IsValidAddress(string address);
        bool IsValidAddressWithHexPrefix(string address);

        Task<bool> IsAllowed(string destinationAddress);
    }

    public class EthereumFacade : IEthereumFacade
    {
        private readonly IEthereumCoreAPI _ethereumApi;
        private readonly ILog _log;
        private readonly static Regex _ethAddressIgnoreCaseRegex = new Regex("^(0x)?[0-9a-f]{40}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly static Regex _ethAddressRegex = new Regex("(0x)?[0-9a-f]{40}$", RegexOptions.Compiled);
        private readonly static Regex _ethAddressCapitalRegex = new Regex("^(0x)?[0-9A-F]{40}$", RegexOptions.Compiled);
        private readonly static Regex _ethAddressWithHexPrefixIgnoreCaseRegex = new Regex("^0x[0-9a-f]{40}$", RegexOptions.Compiled
                                                                                                              | RegexOptions.IgnoreCase);
        private readonly static Regex _ethAddressWithHexPrefixRegex = new Regex("^0x[0-9a-f]{40}$", RegexOptions.Compiled);
        private readonly static Regex _ethAddressWithHexPrefixCapitalRegex = new Regex("^0x[0-9A-F]{40}$", RegexOptions.Compiled);

        private readonly AddressUtil _addressUtil;

        public EthereumFacade(IEthereumCoreAPI ethereumApi, ILogFactory log)
        {
            _ethereumApi = ethereumApi;
            _log = log.CreateLog(this);
            _addressUtil = new AddressUtil();
        }

        public async Task<bool> IsAllowed(string destinationAddress)
        {
            var response = await _ethereumApi.ApiErc20BlackListByAddressGetWithHttpMessagesAsync(destinationAddress);
            EthereumAddressResponse ethereumAddressResponse = response?.Body as EthereumAddressResponse;
            bool isAllowed = ethereumAddressResponse == null || string.IsNullOrEmpty(ethereumAddressResponse.Address);

            return isAllowed;
        }

        public bool IsValidAddress(string address)
        {
            if (!_ethAddressIgnoreCaseRegex.IsMatch(address))
            {
                // check if it has the basic requirements of an address
                return false;
            }
            else if (_ethAddressRegex.IsMatch(address) ||
                     _ethAddressCapitalRegex.IsMatch(address))
            {
                // If it's all small caps or all all caps, return true
                return true;
            }
            else
            {
                // Check each case
                return _addressUtil.IsChecksumAddress(address);
            };
        }

        public bool IsValidAddressWithHexPrefix(string address)
        {
            if (!_ethAddressWithHexPrefixIgnoreCaseRegex.IsMatch(address))
            {
                // check if it has the basic requirements of an address
                return false;
            }
            else if (_ethAddressWithHexPrefixRegex.IsMatch(address) ||
                     _ethAddressWithHexPrefixCapitalRegex.IsMatch(address))
            {
                // If it's all small caps or all all caps, return true
                return true;
            }
            else
            {
                // Check each case
                return _addressUtil.IsChecksumAddress(address);
            };
        }
    }

    public class SolarCoinAddress
    {
        private string _address;
        private static readonly Regex Base58Regex = new Regex(@"^[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]+$");

        public string Value
        {
            get { return _address; }
            set
            {
                SetAddress(value);
            }
        }

        public SolarCoinAddress(string address)
        {
            SetAddress(address);
        }

        private void SetAddress(string address)
        {
            if (!IsValid(address))
                throw new ArgumentException("Address is invalid");

            _address = address;
        }

        public static bool IsValid(string address)
        {
            return !string.IsNullOrEmpty(address) && address[0] == '8' && address.Length < 40 && Base58Regex.IsMatch(address);
        }
    }
}
