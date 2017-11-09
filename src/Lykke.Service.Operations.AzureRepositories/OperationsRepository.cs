using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Common.PasswordTools;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Domain;

namespace Lykke.Service.Operations.AzureRepositories
{    
    public class OperationsRepository : IOperationsRepository
    {
        private string _partitionKey = "Operations";

        private readonly INoSQLTableStorage<OperationEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureIndex> _operationsIndices;

        public OperationsRepository(INoSQLTableStorage<OperationEntity> tableStorage, INoSQLTableStorage<AzureIndex> operationsIndices)
        {
            _tableStorage = tableStorage;
            _operationsIndices = operationsIndices;
        }

        public async Task<IOperation> Get(Guid id)
        {            
            return await _tableStorage.GetDataAsync(_partitionKey, id.ToString());
        }

        public async Task<IEnumerable<IOperation>> Get(Guid clientId, OperationStatus status)
        {
            var indices = await _operationsIndices.GetDataAsync(status.ToString());

            return (await _tableStorage.GetDataAsync(indices.Select(i => new Tuple<string, string>(i.PrimaryPartitionKey, i.PrimaryRowKey)))).Where(t => t.ClientId == clientId);
        }

        public async Task Create(Guid id, Guid clientId, OperationType operationType, string context)
        {
            var transfer = OperationEntity.Create(id, clientId, operationType, context);

            var indexEntry = AzureIndex.Create(OperationStatus.Created.ToString(), id.ToString(), transfer);
            await _operationsIndices.InsertAsync(indexEntry);

            await _tableStorage.InsertAsync(transfer);            
        }

        public async Task UpdateStatus(Guid id, OperationStatus status)
        {
            var operation = await _tableStorage.GetDataAsync(_partitionKey, id.ToString());
            var indexedEntry = AzureIndex.Create(status.ToString(), id.ToString(), operation);

            await _tableStorage.MergeAsync(_partitionKey, id.ToString(), entity =>
            {
                entity.Status = status;
                return entity;
            });

            await _operationsIndices.DeleteAsync(operation.StatusString, id.ToString());            
            await _operationsIndices.InsertAsync(indexedEntry);
        }        
    }
}
