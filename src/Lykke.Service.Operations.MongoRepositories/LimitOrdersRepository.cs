using System;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Operations.Core.Repositories;

namespace Lykke.Service.Operations.Repositories
{
    public class LimitOrderEntity : BaseEntity, ILimitOrder
    {

        public static class ByOrderId
        {
            public static string GeneratePartitionKey()
            {
                return "OrderId";
            }

            public static string GenerateRowKey(string orderId)
            {
                return orderId;
            }

            public static LimitOrderEntity Create(ILimitOrder limitOrder)
            {
                var entity = CreateNew(limitOrder);
                entity.RowKey = GenerateRowKey(limitOrder.Id);
                entity.PartitionKey = GeneratePartitionKey();
                return entity;
            }
        }

        public static class ByClientId
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            public static string GenerateRowKey(string orderId)
            {
                return orderId;
            }

            public static LimitOrderEntity Create(ILimitOrder limitOrder)
            {
                var entity = CreateNew(limitOrder);
                entity.RowKey = GenerateRowKey(limitOrder.Id);
                entity.PartitionKey = GeneratePartitionKey(limitOrder.ClientId);
                return entity;
            }
        }

        public static class ByClientIdActive
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return "Active_" + clientId;
            }

            public static string GenerateRowKey(string orderId)
            {
                return orderId;
            }

            public static LimitOrderEntity Create(ILimitOrder limitOrder)
            {
                var entity = CreateNew(limitOrder);
                entity.RowKey = GenerateRowKey(limitOrder.Id);
                entity.PartitionKey = GeneratePartitionKey(limitOrder.ClientId);
                return entity;
            }
        }

        public static LimitOrderEntity CreateNew(ILimitOrder limitOrder)
        {
            return new LimitOrderEntity
            {
                AssetPairId = limitOrder.AssetPairId,
                ClientId = limitOrder.ClientId,
                CreatedAt = limitOrder.CreatedAt,
                Id = limitOrder.Id,
                Price = limitOrder.Price,
                Status = limitOrder.Status,
                Straight = limitOrder.Straight,
                Volume = limitOrder.Volume,
                RemainingVolume = limitOrder.RemainingVolume,
                MatchingId = limitOrder.MatchingId
            };
        }

        public DateTime CreatedAt { get; set; }

        public double Price { get; set; }
        public string AssetPairId { get; set; }

        public double Volume { get; set; }

        public string Status { get; set; }
        public bool Straight { get; set; }
        public string Id { get; set; }
        public string ClientId { get; set; }

        public double RemainingVolume { get; set; }
        public string MatchingId { get; set; }
    }

    public class LimitOrdersRepository : ILimitOrdersRepository
    {
        private readonly INoSQLTableStorage<LimitOrderEntity> _tableStorage;

        public LimitOrdersRepository(INoSQLTableStorage<LimitOrderEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<ILimitOrder> GetOrderAsync(string orderId)
        {
            return await _tableStorage.GetDataAsync(LimitOrderEntity.ByOrderId.GeneratePartitionKey(), orderId);
        }

        public async Task CreateAsync(ILimitOrder limitOrder)
        {
            limitOrder.Status = LimitOrderStatus.InOrderBook.ToString();

            var byOrderEntity = LimitOrderEntity.ByOrderId.Create(limitOrder);

            // already created by TransactionHandler
            if (!await _tableStorage.CreateIfNotExistsAsync(byOrderEntity))
                return;

            var byClientEntity = LimitOrderEntity.ByClientId.Create(limitOrder);
            var byClientEntityActive = LimitOrderEntity.ByClientIdActive.Create(limitOrder);

            await Task.WhenAll(
                _tableStorage.InsertOrMergeAsync(byClientEntity),
                _tableStorage.InsertOrMergeAsync(byClientEntityActive));
        }

        public Task RemoveAsync(string id, string clientId)
        {
            return Task.WhenAll(
                _tableStorage.DeleteAsync(LimitOrderEntity.ByOrderId.GeneratePartitionKey(), id),
                _tableStorage.DeleteAsync(LimitOrderEntity.ByClientId.GeneratePartitionKey(clientId), id),
                _tableStorage.DeleteAsync(LimitOrderEntity.ByClientIdActive.GeneratePartitionKey(clientId), id)
            );
        }

        public async Task FinishAsync(ILimitOrder order, string status)
        {
            order.Status = status;

            var byOrderEntity = LimitOrderEntity.ByOrderId.Create(order);
            var byClientEntity = LimitOrderEntity.ByClientId.Create(order);

            await _tableStorage.InsertOrMergeAsync(byOrderEntity);
            await _tableStorage.InsertOrMergeAsync(byClientEntity);

            await _tableStorage.DeleteAsync(LimitOrderEntity.ByClientIdActive.GeneratePartitionKey(order.ClientId), order.Id);
        }
    }
}
