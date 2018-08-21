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

    public partial class SwiftCashoutSettingsModel
    {
        /// <summary>
        /// Initializes a new instance of the SwiftCashoutSettingsModel class.
        /// </summary>
        public SwiftCashoutSettingsModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SwiftCashoutSettingsModel class.
        /// </summary>
        public SwiftCashoutSettingsModel(string hotwalletTargetId, string feeTargetId)
        {
            HotwalletTargetId = hotwalletTargetId;
            FeeTargetId = feeTargetId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "HotwalletTargetId")]
        public string HotwalletTargetId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "FeeTargetId")]
        public string FeeTargetId { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (HotwalletTargetId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "HotwalletTargetId");
            }
            if (FeeTargetId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "FeeTargetId");
            }
        }
    }
}
