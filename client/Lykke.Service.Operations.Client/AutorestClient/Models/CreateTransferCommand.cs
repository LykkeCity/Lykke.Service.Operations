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
    using System.Linq;

    public partial class CreateTransferCommand
    {
        /// <summary>
        /// Initializes a new instance of the CreateTransferCommand class.
        /// </summary>
        public CreateTransferCommand()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the CreateTransferCommand class.
        /// </summary>
        public CreateTransferCommand(System.Guid clientId, double amount, System.Guid sourceWalletId, System.Guid walletId, string assetId = default(string))
        {
            ClientId = clientId;
            AssetId = assetId;
            Amount = amount;
            SourceWalletId = sourceWalletId;
            WalletId = walletId;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ClientId")]
        public System.Guid ClientId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "AssetId")]
        public string AssetId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Amount")]
        public double Amount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "SourceWalletId")]
        public System.Guid SourceWalletId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "WalletId")]
        public System.Guid WalletId { get; set; }

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
