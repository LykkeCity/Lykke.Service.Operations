// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class SwiftCashoutAssetModel
    {
        /// <summary>
        /// Initializes a new instance of the SwiftCashoutAssetModel class.
        /// </summary>
        public SwiftCashoutAssetModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SwiftCashoutAssetModel class.
        /// </summary>
        public SwiftCashoutAssetModel(bool kycNeeded, bool swiftCashoutEnabled, string id = default(string), string lykkeEntityId = default(string))
        {
            Id = id;
            KycNeeded = kycNeeded;
            SwiftCashoutEnabled = swiftCashoutEnabled;
            LykkeEntityId = lykkeEntityId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "KycNeeded")]
        public bool KycNeeded { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "SwiftCashoutEnabled")]
        public bool SwiftCashoutEnabled { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "LykkeEntityId")]
        public string LykkeEntityId { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            //Nothing to validate
        }
    }
}
