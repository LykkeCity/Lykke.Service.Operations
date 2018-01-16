using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Client.AutorestClient;
using Lykke.Service.Operations.Contracts;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Client
{
    public class OperationsClient : IOperationsClient, IDisposable
    {
        private OperationsAPI _operationsApi;

        public OperationsClient(string serviceUrl)
        {
            _operationsApi = new OperationsAPI(new Uri(serviceUrl));
        }

        public async Task<OperationModel> Get(Guid id)
        {
            var op = await _operationsApi.ApiOperationsByIdGetAsync(id);

            var result = Mapper.Map<OperationModel>(op);
            result.Context = JObject.FromObject(op.Context);

            return result;
        }

        public async Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status)
        {
            return (await _operationsApi.ApiOperationsByClientIdListByStatusGetAsync(clientId, Mapper.Map<AutorestClient.Models.OperationStatus>(status))).Select(Mapper.Map<OperationModel>);
        }

        public async Task<Guid> Transfer(Guid id, CreateTransferCommand transferCommand)
        {
            return (await _operationsApi.ApiOperationsTransferByIdPostAsync(id, Mapper.Map<AutorestClient.Models.CreateTransferCommand>(transferCommand))).Value;
        }   
               
        public async Task<Guid> NewOrder(Guid id, CreateNewOrderCommand newOrderCommand)
        {
            return (await _operationsApi.ApiOperationsNewOrderByIdPostAsync(id, Mapper.Map<AutorestClient.Models.CreateNewOrderCommand>(newOrderCommand))).Value;
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
