// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Lykke.Service;
    using Lykke.Service.Operations;
    using Lykke.Service.Operations.Client;
    using Lykke.Service.Operations.Client.AutorestClient;
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class GlobalSettingsModel
    {
        /// <summary>
        /// Initializes a new instance of the GlobalSettingsModel class.
        /// </summary>
        public GlobalSettingsModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the GlobalSettingsModel class.
        /// </summary>
        public GlobalSettingsModel(IList<string> blockedAssetPairs, bool bitcoinBlockchainOperationsDisabled, bool btcOperationsDisabled, IcoSettingsModel icoSettings, FeeSettingsModel feeSettings)
        {
            BlockedAssetPairs = blockedAssetPairs;
            BitcoinBlockchainOperationsDisabled = bitcoinBlockchainOperationsDisabled;
            BtcOperationsDisabled = btcOperationsDisabled;
            IcoSettings = icoSettings;
            FeeSettings = feeSettings;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "BlockedAssetPairs")]
        public IList<string> BlockedAssetPairs { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "BitcoinBlockchainOperationsDisabled")]
        public bool BitcoinBlockchainOperationsDisabled { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "BtcOperationsDisabled")]
        public bool BtcOperationsDisabled { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IcoSettings")]
        public IcoSettingsModel IcoSettings { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "FeeSettings")]
        public FeeSettingsModel FeeSettings { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (BlockedAssetPairs == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "BlockedAssetPairs");
            }
            if (IcoSettings == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "IcoSettings");
            }
            if (FeeSettings == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "FeeSettings");
            }
            if (IcoSettings != null)
            {
                IcoSettings.Validate();
            }
            if (FeeSettings != null)
            {
                FeeSettings.Validate();
            }
        }
    }
}
