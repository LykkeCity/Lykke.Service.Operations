// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Lykke.Service;
    using Lykke.Service.Operations;
    using Lykke.Service.Operations.Client;
    using Lykke.Service.Operations.Client.AutorestClient;
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class FeeSettingsCashoutModel
    {
        /// <summary>
        /// Initializes a new instance of the FeeSettingsCashoutModel class.
        /// </summary>
        public FeeSettingsCashoutModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the FeeSettingsCashoutModel class.
        /// </summary>
        public FeeSettingsCashoutModel(IDictionary<string, string> targetClients = default(IDictionary<string, string>))
        {
            TargetClients = targetClients;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TargetClients")]
        public IDictionary<string, string> TargetClients { get; set; }

    }
}