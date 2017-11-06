using System;
using System.Globalization;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Operations.AzureRepositories
{
    public class OperationEntity : TableEntity, IOperation
    {
        public Guid Id => new Guid(RowKey);
        public DateTime Created { get; set; }
        public Guid ClientId { get; set; }

        [IgnoreProperty]
        public OperationType Type
        {
            get => (OperationType) Enum.Parse(typeof(OperationType), TypeString);
            set => TypeString = value.ToString();
        }
        public string TypeString { get; set; }

        [IgnoreProperty]
        public OperationStatus Status
        {
            get => (OperationStatus)Enum.Parse(typeof(OperationStatus), StatusString);
            set => StatusString = value.ToString();
        }
        public string StatusString { get; set; }

        public string AssetId { get; set; }
        [IgnoreProperty]
        public decimal Amount => decimal.TryParse(AmountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0;
        public string AmountString { get; set; }
        public Guid SourceWalletId { get; set; }
        public Guid WalletId { get; set; }

        [IgnoreProperty]        
        public TransferType TransferType
        {
            get => (TransferType)Enum.Parse(typeof(TransferType), TransferTypeString);
            set => TransferTypeString = value.ToString();
        }
        public string TransferTypeString { get; set; }

        public static OperationEntity CreateTransfer(Guid id, TransferType transferType, Guid clientId, string assetId, decimal amount, Guid sourceWalletId, Guid walletId)
        {
            return new OperationEntity
            {
                PartitionKey = "Operations",
                RowKey = id.ToString(),
                Created = DateTime.UtcNow,
                ClientId = clientId,
                Type = OperationType.Transfer,
                Status = OperationStatus.Created,
                TransferType = transferType,
                AssetId = assetId,
                AmountString = amount.ToString(CultureInfo.InvariantCulture),
                SourceWalletId = sourceWalletId,
                WalletId = walletId
            };
        }        
    }
}
