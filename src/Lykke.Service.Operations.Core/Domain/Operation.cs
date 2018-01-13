using System;
using Lykke.Contracts.Operations;

namespace Lykke.Service.Operations.Core.Domain
{
    public class Operation : IHasId
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public Guid ClientId { get; set; }
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }
        public string Context { get; set; }
    }
}
