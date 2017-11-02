using System;
using System.Globalization;
using Common.Platforms;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Operations.AzureRepositories
{
    public class OperationEntity : TableEntity, IOperation
    {
        public Guid Id => new Guid(RowKey);
        public DateTime Created { get; set; }
        public Guid ClientId { get; set; }
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }        
        public string AssetId { get; set; }
        [IgnoreProperty]
        public decimal Amount => decimal.TryParse(AmountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0;
        public string AmountString { get; set; }

        public Guid WalletId { get; set; }

        public static OperationEntity CreateTransfer(Guid id, Guid clientId, string assetId, decimal amount, Guid walletId)
        {
            return new OperationEntity
            {
                PartitionKey = "Transfers",
                RowKey = id.ToString(),
                Created = DateTime.UtcNow,
                ClientId = clientId,
                Type = OperationType.Transfer,
                Status = OperationStatus.Created,
                AssetId = assetId,
                AmountString = amount.ToString(CultureInfo.InvariantCulture),
                WalletId = walletId
            };
        }
    }
}
