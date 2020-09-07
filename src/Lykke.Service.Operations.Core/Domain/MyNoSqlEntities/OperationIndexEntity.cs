using System;
using MyNoSqlServer.Abstractions;

namespace Lykke.Service.Operations.Core.Domain.MyNoSqlEntities
{
    public class OperationIndexEntity : IMyNoSqlDbEntity
    {
        public string Id { get; set; }
        public string ClientId { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string TimeStamp { get; set; }
        public DateTime? Expires { get; set; }

        public static string GetPk(string id) => $"Index_{id}";
        public static string GetRk(string id) => id;

        public static OperationIndexEntity Create(Operation operation, TimeSpan expiration)
        {
            string operationId = operation.Id.ToString();
            return new OperationIndexEntity
            {
                PartitionKey = GetPk(operationId),
                RowKey = GetRk(operationId),
                Id = operationId,
                ClientId = operation.ClientId.ToString(),
                Expires = DateTime.UtcNow.Add(expiration)
            };
        }
    }
}
