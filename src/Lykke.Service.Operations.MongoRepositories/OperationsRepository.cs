using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Domain;
using MongoDB.Driver;

namespace Lykke.Service.Operations.MongoRepositories
{
    public class OperationsRepository : MongoRepository<Operation>, IOperationsRepository
    {
        public OperationsRepository(IMongoDatabase database) : base(database)
        {
        }

        public async Task<IEnumerable<Operation>> Get(Guid clientId, OperationStatus status)
        {
            return await FilterBy(x => x.ClientId == clientId && x.Status == status);
        }

        public async Task Create(Guid id, Guid clientId, OperationType operationType, string context)
        {
            await Add(new Operation { Id = id, ClientId = clientId, Type = operationType, Context = context, Created = DateTime.UtcNow, Status = OperationStatus.Created });
        }

        public async Task UpdateStatus(Guid id, OperationStatus status)
        {
            await GetCollection().UpdateOneAsync(x => x.Id == id, Builders<Operation>.Update.Set("Status", status));
        }

    }
}
