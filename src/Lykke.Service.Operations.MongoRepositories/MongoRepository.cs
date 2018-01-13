﻿using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Lykke.Service.Operations.Core.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace Lykke.Service.Operations.MongoRepositories
{
    public class MongoRepository<T> where T : class, IHasId
    {
        protected readonly IMongoDatabase Database;

        public MongoRepository(IMongoDatabase database)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));

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
        }

        protected IMongoCollection<T> GetCollection()
        {
            return Database.GetCollection<T>(typeof(T).Name);
        }

        public async Task<T> Get(Guid id)
        {
            return await Get(x => x.Id == id).ConfigureAwait(false);
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
            return await GetCollection().Find(expression).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<List<T>> FilterBy(Expression<Func<T, bool>> expression)
        {
            return await GetCollection().Find(expression).ToListAsync();
        }
    }
}