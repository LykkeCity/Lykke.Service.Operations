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

    public partial class OperationModel
    {
        /// <summary>
        /// Initializes a new instance of the OperationModel class.
        /// </summary>
        public OperationModel()
        {
          CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the OperationModel class.
        /// </summary>
        /// <param name="type">Possible values include: 'Transfer',
        /// 'VisaCardPayment', 'MarketOrder', 'LimitOrder', 'NewOrder',
        /// 'CashoutSwift', 'Cashout'</param>
        /// <param name="status">Possible values include: 'Created',
        /// 'Accepted', 'Confirmed', 'Completed', 'Canceled', 'Failed'</param>
        public OperationModel(System.Guid id, System.DateTime created, OperationType type, OperationStatus status, System.Guid clientId, object context = default(object), object activities = default(object), string contextJson = default(string))
        {
            Id = id;
            Created = created;
            Type = type;
            Status = status;
            ClientId = clientId;
            Context = context;
            Activities = activities;
            ContextJson = contextJson;
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
        [JsonProperty(PropertyName = "Created")]
        public System.DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'Transfer',
        /// 'VisaCardPayment', 'MarketOrder', 'LimitOrder', 'NewOrder',
        /// 'CashoutSwift', 'Cashout'
        /// </summary>
        [JsonProperty(PropertyName = "Type")]
        public OperationType Type { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'Created', 'Accepted',
        /// 'Confirmed', 'Completed', 'Canceled', 'Failed'
        /// </summary>
        [JsonProperty(PropertyName = "Status")]
        public OperationStatus Status { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ClientId")]
        public System.Guid ClientId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Context")]
        public object Context { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Activities")]
        public object Activities { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ContextJson")]
        public string ContextJson { get; set; }

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
