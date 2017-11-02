using System;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Operations.Core.Domain;

namespace Lykke.Service.Operations.AzureRepositories
{    
    public class OperationsRepository : IOperationsRepository
    {
        private readonly INoSQLTableStorage<OperationEntity> _tableStorage;

        public OperationsRepository(INoSQLTableStorage<OperationEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IOperation> Get(Guid id)
        {
            return await _tableStorage.GetDataAsync("Transfers", id.ToString());
        }

        public async Task CreateTransfer(Guid id, Guid clientId, string assetId, decimal amount, Guid walletId)
        {
            var transfer = OperationEntity.CreateTransfer(id, clientId, assetId, amount, walletId);

            await _tableStorage.InsertAsync(transfer);            
        }
    }
}
