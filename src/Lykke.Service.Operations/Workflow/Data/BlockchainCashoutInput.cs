using System;
using Lykke.Service.Assets.Client.Models;

namespace Lykke.Service.Operations.Workflow
{
    public class BlockchainCashoutInput
    {
        public Guid OperationId { get; set; }
        public string BlockchainIntegrationLayerId { get; set; }
        public string AssetId { get; set; }
        public long SiriusAssetId { get; set; }
        public string AssetBlockchain { get; set; }
        public bool AssetBlockchainWithdrawal { get; set; }
        public decimal Amount { get; set; }
        public string ToAddress { get; set; }
        public string Tag { get; set; }
        public Guid ClientId { get; set; }
        public string EthHotWallet { get; set; }
        public BlockchainIntegrationType BlockchainIntegrationType { get; set; }
    }
}
