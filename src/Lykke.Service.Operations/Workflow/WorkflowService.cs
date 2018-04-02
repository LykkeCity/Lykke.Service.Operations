using System;
using Common;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;

namespace Lykke.Service.Operations.Workflow
{
    public interface IWorkflowService
    {        
        object GetNeededAsset(Operation context);        
        object AdjustNeededAmount(Operation context);
        object GetWalletBalance(Operation context);        
    }

    public class WorkflowService : IWorkflowService
    {                        
        private readonly IBalancesClient _balancesClient;
        
        public WorkflowService(IBalancesClient balancesClient)
        {                                    
            _balancesClient = balancesClient;            
        }
        
        public object GetNeededAsset(Operation context)
        {
            var orderAction = (OrderAction)context.OperationValues.OrderAction;
            string assetId = context.OperationValues.Asset.Id;
            string baseAssetId = context.OperationValues.AssetPair.BaseAsset.Id;
            string quotingAssetId = context.OperationValues.AssetPair.QuotingAsset.Id;

            if (orderAction == OrderAction.Buy)
            {
                return new
                {                    
                    NeededAssetId = baseAssetId == assetId
                        ? quotingAssetId
                        : baseAssetId,
                    ReceivedAssetId = baseAssetId == assetId
                        ? baseAssetId
                        : quotingAssetId
                };
            }
            else
            {
                return new
                {                    
                    NeededAssetId = baseAssetId == assetId
                        ? baseAssetId
                        : quotingAssetId
                };
            }
        }
        
        public object AdjustNeededAmount(Operation context)
        {
            var orderAction = (OrderAction)context.OperationValues.OrderAction;

            if (orderAction == OrderAction.Buy)
            {
                var balance = (decimal)context.OperationValues.Wallet.Balance;
                int neededAssetAccuracy =
                    (string)context.OperationValues.AssetPair.BaseAsset.Id ==
                    (string)context.OperationValues.NeededAssetId
                        ? context.OperationValues.AssetPair.BaseAsset.Accuracy
                        : context.OperationValues.AssetPair.QuotingAsset.Accuracy;

                var neededAmount = ((decimal)context.OperationValues.NeededAmount.Amount).TruncateDecimalPlaces(neededAssetAccuracy, true);

                return new
                {
                    NeededAmount = new
                    {
                        Amount = context.Type == OperationType.MarketOrder ? Math.Min(balance, neededAmount) : neededAmount
                    }
                };
            }

            return new { };
        }

        public object GetWalletBalance(Operation context)
        {
            var clientId = (string)context.OperationValues.Client.Id;
            var neededAssetId = (string)context.OperationValues.NeededAssetId;

            return _balancesClient.GetClientBalanceByAssetId(new ClientBalanceByAssetIdModel(neededAssetId, clientId)).ConfigureAwait(false).GetAwaiter().GetResult();
        }        
    }
}
