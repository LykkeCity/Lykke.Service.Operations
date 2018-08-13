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

    public partial class ClientCashoutModel
    {
        /// <summary>
        /// Initializes a new instance of the ClientCashoutModel class.
        /// </summary>
        public ClientCashoutModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the ClientCashoutModel class.
        /// </summary>
        public ClientCashoutModel(System.Guid id, decimal balance, bool cashOutBlocked, bool backupDone, string bitcoinAddress = default(string), string multisig = default(string), string kycStatus = default(string))
        {
            Id = id;
            BitcoinAddress = bitcoinAddress;
            Multisig = multisig;
            Balance = balance;
            CashOutBlocked = cashOutBlocked;
            BackupDone = backupDone;
            KycStatus = kycStatus;
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
        [JsonProperty(PropertyName = "BitcoinAddress")]
        public string BitcoinAddress { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Multisig")]
        public string Multisig { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Balance")]
        public decimal Balance { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "CashOutBlocked")]
        public bool CashOutBlocked { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "BackupDone")]
        public bool BackupDone { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "KycStatus")]
        public string KycStatus { get; set; }

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
