using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using MongoDB.Driver;

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

        public async Task Create(Operation operation)
        {            
            await Add(operation);
        }
        
        public async Task UpdateStatus(Guid id, OperationStatus status)
        {
            await GetCollection().UpdateOneAsync(x => x.Id == id, Builders<Operation>.Update.Set("Status", status));
        }

        public async Task<bool> SetClientId(Guid id, Guid clientId)
        {
            var updateResult = await GetCollection().UpdateOneAsync(
                x =>
                    x.Id == id &&
                    x.Status == OperationStatus.Created &&
                    (!x.ClientId.HasValue || x.ClientId.Value == clientId),
                Builders<Operation>.Update
                    .Set("ClientId", clientId)
            );

            return updateResult.MatchedCount != 0;
        }
    }
}
