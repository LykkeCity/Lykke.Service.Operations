//using System;
//using System.Net;
//using System.Threading.Tasks;
//using Common;
//using Common.Log;
//using Lykke.Contracts.Operations;
//using Lykke.Service.Balances.AutorestClient.Models;
//using Lykke.Service.Balances.Client;
//using Lykke.Service.ClientAccount.Client;
//using Lykke.Service.ExchangeOperations.Client;
//using Lykke.Service.Kyc.Abstractions.Services;
//using Lykke.Service.Limitations.Client;
//using Lykke.Service.Operations.Core.Domain;
//using Lykke.Service.Operations.Core.Repositories;
//using Lykke.Service.Operations.Models;
//using Lykke.Service.Operations.Services;
//using Microsoft.AspNetCore.Mvc;
//using Newtonsoft.Json;
//using Swashbuckle.AspNetCore.SwaggerGen;

//namespace Lykke.Service.Operations.Controllers
//{
//    public class CashoutController : Controller
//    {
//        private readonly ILog _log;
//        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepositry;
//        private readonly CachedAssetsDictionary _assets;
//        private readonly IKycStatusService _kycStatusService;
//        private readonly IClientAccountClient _clientAccountClient;
//        private readonly IBalancesClient _balancesClient;
//        private readonly ILimitationsServiceClient _limitationsServiceClient;
//        private readonly IExchangeOperationsServiceClient _exchangeOperationsServiceClient;

//        public CashoutController(
//            ILog log, 
//            IAppGlobalSettingsRepositry appGlobalSettingsRepositry, 
//            CachedAssetsDictionary assets,
//            IKycStatusService kycStatusService,
//            IClientAccountClient clientAccountClient,
//            IBalancesClient balancesClient,
//            ILimitationsServiceClient limitationsServiceClient,
//            IExchangeOperationsServiceClient exchangeOperationsServiceClient)
//        {
//            _log = log;
//            _appGlobalSettingsRepositry = appGlobalSettingsRepositry;
//            _assets = assets;
//            _kycStatusService = kycStatusService;
//            _clientAccountClient = clientAccountClient;
//            _balancesClient = balancesClient;
//            _limitationsServiceClient = limitationsServiceClient;
//            _exchangeOperationsServiceClient = exchangeOperationsServiceClient;
//        }

//        [ProducesResponseType((int)HttpStatusCode.OK)]        
//        [HttpPost("cashout/{id}")]
//        [SwaggerOperation("Cashout")]
//        public async Task CashoutAsync(Guid id, [FromBody]HotWalletCashoutOperation model)
//        {
//            #region Validation

//            var clientId = model.ClientId;

//            var globalSettings = await _appGlobalSettingsRepositry.GetAsync();                        

//            var context = new
//            {                
//                model.AssetId,
//                model.Volume,
//                model.DestinationAddress,                                
//                Asset = await _assets.GetItemAsync(model.AssetId),                
//                Client = new
//                {
//                    KycStatus = await _kycStatusService.GetKycStatusAsync(clientId),
//                    CashOutBlockedSettings = await _clientAccountClient.GetCashOutBlockAsync(clientId),
//                    BackupSettings = await _clientAccountClient.GetBackupAsync(clientId),                    
//                    Balance = await _balancesClient.GetClientBalanceByAssetId(new ClientBalanceByAssetIdModel(model.AssetId, clientId))
//                },
//                GlobalContext = new
//                {
//                    globalSettings.CashOutBlocked,
//                    globalSettings.LowCashOutLimit,
//                    globalSettings.LowCashOutTimeoutMins
//                },               
//            };

//            var operation = new Operation
//            {
//                Id = id,
//                ClientId = new Guid(model.ClientId),
//                Created = DateTime.UtcNow,
//                Status = OperationStatus.Created,
//                Type = OperationType.Cashout,
//                Context = JsonConvert.SerializeObject(context, Formatting.Indented)
//            };

//            #endregion

//            //var ethAsset = await _srvEthereumHelper.GetEthAsset();
//            //if (ethAsset.Id == model.AssetId)
//            //{
//            //    var hotWalletAddress = _ethereumSettings.HotwalletAddress;
//            //    var currentBalance = await _srvEthereumHelper.GetBalanceOnAdapterAsync(asset, hotWalletAddress);

//            //    if (currentBalance.HasError)
//            //    {
//            //        await
//            //            _log.WriteWarningAsync(nameof(EthereumController), nameof(CashoutAsync), currentBalance.ToJson(),
//            //                "Balance read operation failed");

//            //        return ResponseModel.CreateFail(ResponseModel.ErrorCodeType.RuntimeProblem,
//            //            Phrases.TechnicalProblems);
//            //    }

//            //    if (model.Volume > currentBalance.Result.Balance)
//            //    {                    
//            //        return ResponseModel.CreateFail(ResponseModel.ErrorCodeType.PreviousTransactionsWereNotCompleted,
//            //            Phrases.PreviousTransactionsWereNotCompleted);
//            //    }

//            //    var estimationRespone = await _srvEthereumHelper.EstimateCashOutAsync(Guid.NewGuid(),
//            //        string.Empty,
//            //        asset,
//            //        hotWalletAddress,
//            //        model.DestinationAddress,
//            //        model.Volume);

//            //    if (estimationRespone.HasError)
//            //    {
//            //        await
//            //            _log.WriteWarningAsync(nameof(EthereumController), nameof(CashoutAsync), estimationRespone.ToJson(),
//            //                "Ethereum Core estimation operation failed");

//            //        return ProcessErrorMessage<EthereumSuccessTradeRespModel>(estimationRespone.Error);
//            //    }

//            //    if (!estimationRespone.Result.IsAllowed)
//            //        return ResponseModel.CreateFail(ResponseModel.ErrorCodeType.InvalidInputField, Phrases.CashoutIsNotAllowed);
//            //}


//            var checkResult = await _limitationsServiceClient.CheckAsync(
//                clientId,
//                model.AssetId,
//                (double)model.Volume,
//                CurrencyOperationType.CryptoCashOut);
            
//            var res = await _exchangeOperationsServiceClient.CashOutAsync(
//                clientId,
//                model.DestinationAddress,
//                (double)model.Volume,
//                model.AssetId,
//                txId: Guid.NewGuid().ToString());

//            return;
//        }
//    }
//}
