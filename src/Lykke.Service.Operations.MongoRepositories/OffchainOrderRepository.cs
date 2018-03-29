using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Operations.Core.Repositories;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Operations.MongoRepositories
{
    public class BaseEntity : TableEntity
    {
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);

            foreach (var p in GetType().GetProperties().Where(x =>
                (x.PropertyType == typeof(decimal) || x.PropertyType == typeof(decimal?)) && properties.ContainsKey(x.Name)))
            {
                var value = properties[p.Name].StringValue;
                p.SetValue(this, value != null ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) : (decimal?)null);
            }

            foreach (var p in GetType().GetProperties().Where(x => x.PropertyType.GetTypeInfo().IsEnum && properties.ContainsKey(x.Name)))
                p.SetValue(this, Enum.ToObject(p.PropertyType, properties[p.Name].Int32Value));

        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = base.WriteEntity(operationContext);

            foreach (var p in GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal) || x.PropertyType == typeof(decimal?)))
                properties.Add(p.Name, new EntityProperty(p.GetValue(this)?.ToString()));

            foreach (var p in GetType().GetProperties().Where(x => x.PropertyType.GetTypeInfo().IsEnum))
                properties.Add(p.Name, new EntityProperty((int)p.GetValue(this)));

            return properties;
        }
    }

    public class OffchainOrder : BaseEntity, IOffchainOrder
    {
        public string Id => RowKey;

        public string OrderId { get; set; }
        public string ClientId { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Volume { get; set; }
        public decimal ReservedVolume { get; set; }
        public string AssetPair { get; set; }
        public string Asset { get; set; }
        public bool Straight { get; set; }
        public decimal Price { get; set; }
        public bool IsLimit { get; set; }


        public static string GeneratePartitionKey()
        {
            return "Order";
        }

        public static OffchainOrder Create(string clientId, string asset, string assetPair, decimal volume, decimal reservedVolume,
            bool straight, decimal price = 0)
        {
            var id = Guid.NewGuid().ToString();
            return new OffchainOrder
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = id,
                OrderId = id,
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow,
                Volume = volume,
                ReservedVolume = reservedVolume,
                AssetPair = assetPair,
                Asset = asset,
                Straight = straight,
                Price = price,
                IsLimit = price > 0
            };
        }
    }

    public class OffchainOrderRepository : IOffchainOrdersRepository
    {
        private readonly INoSQLTableStorage<OffchainOrder> _storage;

        public OffchainOrderRepository(INoSQLTableStorage<OffchainOrder> storage)
        {
            _storage = storage;
        }

        public async Task<IOffchainOrder> GetOrder(string id)
        {
            return await _storage.GetDataAsync(OffchainOrder.GeneratePartitionKey(), id);
        }

        public async Task<IOffchainOrder> CreateOrder(string clientId, string asset, string assetPair, decimal volume, decimal reservedVolume, bool straight)
        {
            var entity = OffchainOrder.Create(clientId, asset, assetPair, volume, reservedVolume, straight);
            await _storage.InsertAsync(entity);
            return entity;
        }

        public async Task<IOffchainOrder> CreateLimitOrder(string clientId, string asset, string assetPair, decimal volume, decimal reservedVolume, bool straight, decimal price)
        {
            var entity = OffchainOrder.Create(clientId, asset, assetPair, volume, reservedVolume, straight, price);
            await _storage.InsertAsync(entity);
            return entity;
        }

        public Task UpdatePrice(string orderId, decimal price)
        {
            return _storage.ReplaceAsync(OffchainOrder.GeneratePartitionKey(), orderId, order =>
            {
                order.Price = price;
                return order;
            });
        }
    }
}
