using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using JetBrains.Annotations;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Client.AutorestClient;
using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Client
{
    public sealed class OperationsClient : IOperationsClient, IDisposable
    {
        private OperationsAPI _operationsApi;
        private readonly IMapper _mapper;

        public OperationsClient([NotNull] string serviceUrl)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ClientAutomapperProfile>();
            });
            _mapper = config.CreateMapper();
            _operationsApi = new OperationsAPI(new Uri(serviceUrl));
        }

        public async Task<OperationModel> Get(Guid id)
        {
            var op = await _operationsApi.ApiOperationsByIdGetAsync(id);

            var result = _mapper.Map<OperationModel>(op);

            return result;
        }

        public async Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status)
        {
            return (await _operationsApi.ApiOperationsByClientIdListByStatusGetAsync(clientId, _mapper.Map<AutorestClient.Models.OperationStatus>(status))).Select(_mapper.Map<OperationModel>);
        }

        public async Task<Guid> Transfer(Guid id, CreateTransferCommand transferCommand)
        {
            return (await _operationsApi.ApiOperationsTransferByIdPostAsync(id, _mapper.Map<AutorestClient.Models.CreateTransferCommand>(transferCommand))).Value;
        }

        public async Task<Guid> NewOrder(Guid id, CreateNewOrderCommand newOrderCommand)
        {
            return (await _operationsApi.ApiOperationsNewOrderByIdPostAsync(id, _mapper.Map<AutorestClient.Models.CreateNewOrderCommand>(newOrderCommand))).Value;
        }

        public Task Cancel(Guid id)
        {
            return _operationsApi.ApiOperationsCancelByIdPostAsync(id);
        }

        public Task Complete(Guid id)
        {
            return _operationsApi.ApiOperationsCompleteByIdPostAsync(id);
        }

        public Task Confirm(Guid id)
        {
            return _operationsApi.ApiOperationsConfirmByIdPostAsync(id);
        }

        public Task Fail(Guid id)
        {
            return _operationsApi.ApiOperationsFailByIdPostAsync(id);
        }

        public void Dispose()
        {
            if (_operationsApi == null)
                return;
            _operationsApi.Dispose();
            _operationsApi = null;
        }
    }
}
