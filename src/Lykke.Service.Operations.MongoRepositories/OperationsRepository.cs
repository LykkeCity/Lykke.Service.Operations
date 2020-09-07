using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using MongoDB.Driver;

namespace Lykke.Service.Operations.Repositories
{
    public class OperationsRepository : MongoRepository<Operation>, IOperationsRepository
    {
        public OperationsRepository(IMongoDatabase database, ILogFactory logFactory) : base(database, logFactory)
        {
            GetCollection().Indexes.CreateOneAsync(Builders<Operation>.IndexKeys.Ascending(_ => _.ClientId).Ascending(_ => _.Status).Ascending(_ => _.Type));
        }

        public async Task<IEnumerable<Operation>> Get(Guid? clientId, OperationStatus? status, OperationType? type, int? skip = 0, int? take = 10)
        {
            var result = await RetryPolicy.Execute(async () =>
            {
                var res = await GetCollection()
                  .Find(x => (clientId == null || x.ClientId == clientId) && (status == null || x.Status == status) && (type == null|| x.Type == type))
                  .SortByDescending(x => x.Created)
                  .Skip(skip)
                  .Limit(take)
                  .ToListAsync();

                return res;
            });

            return result;
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
