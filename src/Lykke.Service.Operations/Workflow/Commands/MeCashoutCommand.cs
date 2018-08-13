using System;
using Lykke.Service.FeeCalculator.AutorestClient.Models;
using MessagePack;

namespace Lykke.Service.Operations.Workflow.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class MeCashoutCommand
    {
        public Guid OperationId { get; set; }
        public Guid RequestId { get; set; }
        public string ClientId { get; set; }
        public string AssetId { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal Amount { get; set; }
        public string FeeClientId { get; set; }
        public double FeeSize { get; set; }
        public FeeType FeeType { get; set; }
    }
}
