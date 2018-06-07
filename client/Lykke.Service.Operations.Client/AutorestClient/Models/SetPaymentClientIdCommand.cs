// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class SetPaymentClientIdCommand
    {
        /// <summary>
        /// Initializes a new instance of the SetPaymenClientIdCommand class.
        /// </summary>
        public SetPaymentClientIdCommand()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SetPaymenClientIdCommand class.
        /// </summary>
        public SetPaymentClientIdCommand(System.Guid clientId)
        {
            ClientId = clientId;
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
