// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class SwiftCashoutFeeModel
    {
        /// <summary>
        /// Initializes a new instance of the SwiftCashoutFeeModel class.
        /// </summary>
        public SwiftCashoutFeeModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SwiftCashoutFeeModel class.
        /// </summary>
        public SwiftCashoutFeeModel(string hotwalletTargetId = default(string), string feeTargetId = default(string))
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

    }
}