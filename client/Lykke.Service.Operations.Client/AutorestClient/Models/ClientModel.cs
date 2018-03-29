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

    public partial class ClientModel
    {
        /// <summary>
        /// Initializes a new instance of the ClientModel class.
        /// </summary>
        public ClientModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ClientModel class.
        /// </summary>
        public ClientModel(System.Guid id, bool tradesBlocked, bool backupDone, string kycStatus = default(string), PersonalDataModel personalData = default(PersonalDataModel))
        {
            Id = id;
            TradesBlocked = tradesBlocked;
            BackupDone = backupDone;
            KycStatus = kycStatus;
            PersonalData = personalData;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Id")]
        public System.Guid Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "TradesBlocked")]
        public bool TradesBlocked { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "BackupDone")]
        public bool BackupDone { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "KycStatus")]
        public string KycStatus { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "PersonalData")]
        public PersonalDataModel PersonalData { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
        }
    }
}
