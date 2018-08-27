using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using MongoDB.Driver;

namespace Lykke.Service.Operations.Repositories
{
    public class OperationsRepository : MongoRepository<Operation>, IOperationsRepository
    {
        public OperationsRepository(IMongoDatabase database) : base(database)
        {
            GetCollection().Indexes.CreateOneAsync(Builders<Operation>.IndexKeys.Ascending(_ => _.ClientId).Ascending(_ => _.Status).Ascending(_ => _.Type));
        }

        public async Task<IEnumerable<Operation>> Get(Guid? clientId, OperationStatus? status, OperationType? type)
        {
            return await FilterBy(x => (!clientId.HasValue || x.ClientId == clientId) && (!status.HasValue || x.Status == status) && (type.HasValue || x.Type == type));
        }

        public async Task Create(Operation operation)
        {            
            await Add(operation);
        }
        
        public async Task UpdateStatus(Guid id, OperationStatus status)
        {
            await GetCollection().UpdateOneAsync(x => x.Id == id, Builders<Operation>.Update.Set("Status", status));
        }

    }
}
