using System;
using Lykke.Contracts.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Models
{
    public class OperationModel
    {
        public Guid Id { get; set; }

        public DateTime Created { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OperationType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OperationStatus Status { get; set; }

        public Guid ClientId { get; set; }
        public JObject Context { get; set; }        
    }
}
