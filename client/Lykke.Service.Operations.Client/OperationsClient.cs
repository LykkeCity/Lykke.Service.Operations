using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Client.AutorestClient;
using Lykke.Service.Operations.Contracts;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Client
{
    public class OperationsClient : IOperationsClient, IDisposable
    {        
        private OperationsAPI _operationsApi;
        private readonly IMapper _mapper;

        public OperationsClient(string serviceUrl)
        {            
            _operationsApi = new OperationsAPI(new Uri(serviceUrl));

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<AutorestClient.Models.OperationModel, OperationModel>();
                cfg.CreateMap<OperationStatus, AutorestClient.Models.OperationStatus>();
                cfg.CreateMap<CreateTransferCommand, AutorestClient.Models.CreateTransferCommand>();
            });            
            _mapper = config.CreateMapper();
        }

        public async Task<OperationModel> Get(Guid id)
        {
            var op = await _operationsApi.ApiOperationsByIdGetAsync(id);

            var result =  _mapper.Map<OperationModel>(op);
            result.Context = JObject.FromObject(op.Context);

            return result;
        }

        public async Task<IEnumerable<OperationModel>> Get(Guid clientId, OperationStatus status)
        {
            return (await _operationsApi.ApiOperationsByClientIdListByStatusGetAsync(clientId, _mapper.Map<AutorestClient.Models.OperationStatus>(status))).Select(m => _mapper.Map<OperationModel>(m));
        }

        public async Task<Guid> Transfer(Guid id, CreateTransferCommand transferCommand)
        {
            return (await _operationsApi.ApiOperationsTransferByIdPostAsync(id, _mapper.Map<AutorestClient.Models.CreateTransferCommand>(transferCommand))).Value;
        }

        public async Task Cancel(Guid id)
        {
            await _operationsApi.ApiOperationsCancelByIdPostAsync(id);
        }

        public async Task Complete(Guid id)
        {
            await _operationsApi.ApiOperationsCompleteByIdPostAsync(id);
        }

        public async Task Confirm(Guid id)
        {
            await _operationsApi.ApiOperationsConfirmByIdPostAsync(id);
        }

        public async Task Fail(Guid id)
        {
            await _operationsApi.ApiOperationsFailByIdPostAsync(id);
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
