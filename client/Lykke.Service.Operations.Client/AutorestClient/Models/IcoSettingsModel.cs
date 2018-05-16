// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class IcoSettingsModel
    {
        /// <summary>
        /// Initializes a new instance of the IcoSettingsModel class.
        /// </summary>
        public IcoSettingsModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the IcoSettingsModel class.
        /// </summary>
        public IcoSettingsModel(IList<string> restrictedCountriesIso3 = default(IList<string>), string lKK2YAssetId = default(string))
        {
            RestrictedCountriesIso3 = restrictedCountriesIso3;
            LKK2YAssetId = lKK2YAssetId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "RestrictedCountriesIso3")]
        public IList<string> RestrictedCountriesIso3 { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "LKK2YAssetId")]
        public string LKK2YAssetId { get; set; }

    }
}
