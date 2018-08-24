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
    using System.Linq;

    public partial class CreateSwiftCashoutCommand
    {
        /// <summary>
        /// Initializes a new instance of the CreateSwiftCashoutCommand class.
        /// </summary>
        public CreateSwiftCashoutCommand()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the CreateSwiftCashoutCommand class.
        /// </summary>
        public CreateSwiftCashoutCommand(SwiftCashoutAssetModel asset, double volume, SwiftCashoutClientModel client, SwiftFieldsModel swift, SwiftCashoutSettingsModel cashoutSettings)
        {
            Asset = asset;
            Volume = volume;
            Client = client;
            Swift = swift;
            CashoutSettings = cashoutSettings;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Asset")]
        public SwiftCashoutAssetModel Asset { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Volume")]
        public double Volume { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Client")]
        public SwiftCashoutClientModel Client { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Swift")]
        public SwiftFieldsModel Swift { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "CashoutSettings")]
        public SwiftCashoutSettingsModel CashoutSettings { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Asset == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Asset");
            }
            if (Client == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Client");
            }
            if (Swift == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Swift");
            }
            if (CashoutSettings == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "CashoutSettings");
            }
            if (Asset != null)
            {
                Asset.Validate();
            }
            if (Client != null)
            {
                Client.Validate();
            }
            if (Swift != null)
            {
                Swift.Validate();
            }
            if (CashoutSettings != null)
            {
                CashoutSettings.Validate();
            }
        }
    }
}
