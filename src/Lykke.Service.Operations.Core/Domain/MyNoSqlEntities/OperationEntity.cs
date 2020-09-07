using System;
using System.Collections.Generic;
using Lykke.Service.Operations.Contracts;
using Lykke.Workflow;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Core.Domain.MyNoSqlEntities
{
    public class OperationEntity : IMyNoSqlDbEntity
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }
        public Guid ClientId { get; set; }
        public JObject Context { get; set; }
        public List<OperationActivity> Activities { get; set; }
        public string ContextJson { get; set; }
        public WorkflowState WorkflowState { get; set; }
        public string InputValues { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }

        public static string GetPk(string clientId) => clientId;
        public static string GetRk(string id) => id;

        public static OperationEntity Create(Operation operation, TimeSpan expiration)
        {
            return new OperationEntity
            {
                PartitionKey = GetPk(operation.ClientId.ToString()),
                RowKey = GetRk(operation.Id.ToString()),
                Id = operation.Id,
                Created = operation.Created,
                Type = operation.Type,
                Status = operation.Status,
                ClientId = operation.ClientId,
                Context = JObject.Parse(operation.Context),
                Activities = operation.Activities,
                ContextJson = operation.Context,
                Expires = DateTime.UtcNow.Add(expiration)
            };
        }
    }
}
