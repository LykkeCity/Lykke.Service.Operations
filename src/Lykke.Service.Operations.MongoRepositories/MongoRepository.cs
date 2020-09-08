using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Operations.Core.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using Polly;

namespace Lykke.Service.Operations.Repositories
{
    public class MongoRepository<T> where T : class, IHasId
    {
        protected readonly IMongoDatabase Database;
        protected readonly Policy RetryPolicy;
        private readonly ILog _log;

        public MongoRepository(
            IMongoDatabase database,
            ILogFactory logFactory
            )
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            _log = logFactory.CreateLog(this);

            MongoDefaults.GuidRepresentation = GuidRepresentation.Standard;
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                    cm.SetIdMember(cm.GetMemberMap(x => x.Id)
                        .SetIdGenerator(GuidGenerator.Instance));
                });
            }

            RetryPolicy = Policy
                .Handle<MongoConnectionException>(exception =>
                {
                    _log.Warning("Retry on MongoConnectionException", context: new { connectionId = exception.ConnectionId.ToJson(), Data = exception.Data.ToJson()}.ToJson());
                    return true;
                })
                .Or<TaskCanceledException>(exception =>
                {
                    _log.Warning("Retry on TaskCanceledException", context: new { exception.Message, Data = exception.Data.ToJson()}.ToJson());
                    return true;
                }).Or<SocketException>(exception =>
                {
                    _log.Warning("Retry on SocketException", context: new { exception.ErrorCode, exception.SocketErrorCode, Data = exception.Data.ToJson()}.ToJson());
                    return true;
                })
                .Or<TimeoutException>(exception =>
                {
                    _log.Warning("Retry on TimeoutException", context: new { exception.Message }.ToJson());
                    return true;
                })
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        protected IMongoCollection<T> GetCollection()
        {
            return Database.GetCollection<T>(typeof(T).Name);
        }

        public async Task<T> Get(Guid id)
        {
            var result = await Get(x => x.Id == id).ConfigureAwait(false);
            return result;
        }

        public async Task Add(T entity)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
            await GetCollection().InsertOneAsync(entity).ConfigureAwait(false);
        }

        public async Task Add(IEnumerable<T> items)
        {
            var entities = items.ToList();
            foreach (var entity in entities)
            {
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.NewGuid();
                }
            }
            await GetCollection().InsertManyAsync(entities).ConfigureAwait(false);
        }

        public async Task Update(T entity)
        {
            await GetCollection().ReplaceOneAsync(new BsonDocument("_id", entity.Id), entity).ConfigureAwait(false);
        }

        public async Task Save(T entity)
        {
            await GetCollection().ReplaceOneAsync(new BsonDocument("_id", entity.Id), entity, new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);
        }

        public async Task Update(IEnumerable<T> items)
        {
            await items.ParallelForEachAsync(async item =>
            {
                await Update(item).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public async Task Delete(T entity)
        {
            await GetCollection().DeleteOneAsync(new BsonDocument("_id", entity.Id)).ConfigureAwait(false);
        }

        public async Task Delete(IEnumerable<T> entities)
        {
            await entities.ParallelForEachAsync(async item =>
            {
                await Delete(item).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public IQueryable<T> All()
        {
            return GetCollection().AsQueryable();
        }

        public async Task<T> Get(Expression<Func<T, bool>> expression)
        {
            var result = await RetryPolicy.ExecuteAsync(async () =>
            {
                var res = await GetCollection().Find(expression).FirstOrDefaultAsync().ConfigureAwait(false);

                return res;
            });

            return result;
        }

        public async Task<List<T>> FilterBy(Expression<Func<T, bool>> expression)
        {
            var result = await RetryPolicy.ExecuteAsync(async () =>
            {
                var res = await GetCollection().Find(expression).ToListAsync();

                return res;
            });

            return result;
        }
    }
}
