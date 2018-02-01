using System;
using Lykke.Contracts.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Operations.Contracts
{
    public class OperationModel
    {
        /// <summary>
        /// An Id of the operation
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// A time when the operation was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Operation type
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationType Type { get; set; }

        /// <summary>
        /// Current operation status
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationStatus Status { get; set; }

        /// <summary>
        /// A client ID
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// Addition information related to the operation. Currently <see cref="NewOrderContext"/> or <see cref="TransferContext"/>/>
        /// </summary>
        public string Context { get; set; }
    }
}
