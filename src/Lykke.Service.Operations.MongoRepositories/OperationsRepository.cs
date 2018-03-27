using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Domain;
using MongoDB.Driver;
using OperationType = Lykke.Service.Operations.Contracts.OperationType;

namespace Lykke.Service.Operations.MongoRepositories
{
    public class OperationsRepository : MongoRepository<Operation>, IOperationsRepository
    {
        public OperationsRepository(IMongoDatabase database) : base(database)
        {
            GetCollection().Indexes.CreateOneAsync(Builders<Operation>.IndexKeys.Ascending(_ => _.ClientId).Ascending(_ => _.Status));
        }

        public async Task<IEnumerable<Operation>> Get(Guid clientId, OperationStatus status)
        {
            return await FilterBy(x => x.ClientId == clientId && x.Status == status);
        }

        public async Task Save(Operation operation)
        {
            await Add(operation);
        }

        public async Task UpdateStatus(Guid id, OperationStatus status)
        {
            await GetCollection().UpdateOneAsync(x => x.Id == id, Builders<Operation>.Update.Set("Status", status));
        }

    }
}
