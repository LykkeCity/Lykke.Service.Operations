using System;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Operations.AzureRepositories
{
    public class OperationEntity : TableEntity, IOperation
    {
        public Guid Id => new Guid(RowKey);
        public DateTime Created { get; set; }
        public Guid ClientId { get; set; }
        public string Context { get; set; }

        [IgnoreProperty]
        public OperationType Type
        {
            get => (OperationType) Enum.Parse(typeof(OperationType), TypeString);
            set => TypeString = value.ToString();
        }
        public string TypeString { get; set; }

        [IgnoreProperty]
        public OperationStatus Status
        {
            get => (OperationStatus)Enum.Parse(typeof(OperationStatus), StatusString);
            set => StatusString = value.ToString();
        }
        public string StatusString { get; set; }        
        
        public static OperationEntity Create(Guid id, Guid clientId, OperationType operationType, string context)
        {
            return new OperationEntity
            {
                PartitionKey = "Operations",
                RowKey = id.ToString(),
                Created = DateTime.UtcNow,
                ClientId = clientId,
                Type = operationType,
                Status = OperationStatus.Created,
                Context = context                
            };
        }        
    }
}
