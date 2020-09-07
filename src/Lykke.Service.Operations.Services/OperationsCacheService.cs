using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Domain.MyNoSqlEntities;
using Lykke.Service.Operations.Core.Services;
using MyNoSqlServer.Abstractions;

namespace Lykke.Service.Operations.Services
{
    public class OperationsCacheService : IOperationsCacheService
    {
        private readonly IMyNoSqlServerDataReader<OperationEntity> _reader;
        private readonly IMyNoSqlServerDataReader<OperationIndexEntity> _indexReader;
        private readonly IMyNoSqlServerDataWriter<OperationEntity> _writer;
        private readonly IMyNoSqlServerDataWriter<OperationIndexEntity> _indexWriter;
        private readonly IOperationsRepository _repository;
        private readonly IMapper _mapper;
        private readonly TimeSpan _operationExpiration = TimeSpan.FromHours(1);
        private const int OperationsInCacheCount = 20;

        public OperationsCacheService(
            IMyNoSqlServerDataReader<OperationEntity> reader,
            IMyNoSqlServerDataReader<OperationIndexEntity> indexReader,
            IMyNoSqlServerDataWriter<OperationEntity> writer,
            IMyNoSqlServerDataWriter<OperationIndexEntity> indexWriter,
            IOperationsRepository repository,
            IMapper mapper
            )
        {
            _reader = reader;
            _indexReader = indexReader;
            _writer = writer;
            _indexWriter = indexWriter;
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<Operation> GetAsync(Guid id)
        {
            string operationId = id.ToString();
            var index = _indexReader.Get(OperationIndexEntity.GetPk(operationId), OperationIndexEntity.GetRk(operationId));

            if (index != null)
            {
                var entity = _reader.Get(OperationEntity.GetPk(index.ClientId), OperationEntity.GetRk(operationId));

                if (entity != null)
                    return _mapper.Map<Operation>(entity);
            }

            var operation = await _repository.Get(id);

            if (operation != null)
            {
                await _writer.InsertOrReplaceAsync(OperationEntity.Create(operation, _operationExpiration));
                await _indexWriter.InsertOrReplaceAsync(OperationIndexEntity.Create(operation, _operationExpiration));
            }

            return operation;
        }

        public async Task<IEnumerable<Operation>> GetAsync(Guid clientId, OperationStatus? status, OperationType? type, int? skip, int? take)
        {
            if (skip.HasValue && take.HasValue)
            {
                var entities = _reader.Get(OperationEntity.GetPk(clientId.ToString()))
                    .Where(x => (status == null || x.Status == status.Value) && (type == null || x.Type == type.Value)).ToList();

                if (entities.Count >= skip.Value + take.Value)
                {
                    return _mapper.Map<List<Operation>>(entities.OrderByDescending(x => x.Created).Skip(skip.Value).Take(take.Value));
                }
            }

            var operations = (await _repository.Get(clientId, status, type, skip, take)).ToList();

            var newEntitites = operations.Select(x => OperationEntity.Create(x, _operationExpiration));
            var indexes = operations.Select(x => OperationIndexEntity.Create(x, _operationExpiration));

            await _writer.BulkInsertOrReplaceAsync(newEntitites);
            await _indexWriter.BulkInsertOrReplaceAsync(indexes);

            return operations;
        }

        public async Task CreateAsync(Operation operation)
        {
            if (operation.Id == Guid.Empty)
            {
                operation.Id = Guid.NewGuid();
            }

            await _repository.Save(operation);

            await _writer.InsertOrReplaceAsync(OperationEntity.Create(operation, _operationExpiration));
            await _indexWriter.InsertOrReplaceAsync(OperationIndexEntity.Create(operation, _operationExpiration));
            var entities = _reader.Get(OperationEntity.GetPk(operation.ClientId.ToString()));

            if (entities.Count > OperationsInCacheCount)
            {
                var oldEntity = entities.OrderBy(x => x.Created).Last();
                await _writer.DeleteAsync(oldEntity.PartitionKey, oldEntity.RowKey);
                await _indexWriter.DeleteAsync(OperationIndexEntity.GetPk(oldEntity.RowKey), oldEntity.RowKey);
            }
        }

        public async Task UpdateStatusAsync(Guid id, OperationStatus status)
        {
            await _repository.UpdateStatus(id, status);
            var index = _indexReader.Get(OperationIndexEntity.GetPk(id.ToString()),
                OperationIndexEntity.GetRk(id.ToString()));

            if (index != null)
            {
                var entity = _reader.Get(OperationEntity.GetPk(index.ClientId), OperationEntity.GetRk(id.ToString()));
                if (entity != null)
                {
                    var operation = _mapper.Map<Operation>(entity);
                    operation.Status = status;
                    await _writer.InsertOrReplaceAsync(OperationEntity.Create(operation, _operationExpiration));
                    await _indexWriter.InsertOrReplaceAsync(OperationIndexEntity.Create(operation, _operationExpiration));
                }
            }
        }

        public async Task SaveAsync(Operation operation)
        {
            await _repository.Save(operation);
            await _writer.InsertOrReplaceAsync(OperationEntity.Create(operation, _operationExpiration));
            await _indexWriter.InsertOrReplaceAsync(OperationIndexEntity.Create(operation, _operationExpiration));
        }

        public async Task ClearAsync()
        {
            var operations = _reader.Get();

            foreach (var operation in operations)
            {
                if (operation.Expires.HasValue && operation.Expires.Value > DateTime.UtcNow)
                {
                    await _writer.DeleteAsync(operation.PartitionKey, operation.RowKey);
                    await _indexWriter.DeleteAsync(OperationIndexEntity.GetPk(operation.RowKey),
                        OperationIndexEntity.GetRk(operation.RowKey));
                }
            }
        }
    }
}
