using System;
using Lykke.Service.FeeCalculator.AutorestClient.Models;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class CashoutMeInput
    {
        public Guid OperationId { get; set; }
        public Guid RequestId { get; set; }
        public string ClientId { get; set; }        
        public decimal Volume { get; set; }
        public string AssetId { get; set; }
        public int AssetAccuracy { get; set; }
        public double FeeSize { get; set; }
        public FeeType FeeType { get; set; }
        public string CashoutTargetClientId { get; set; }
        public string DestinationAddress { get; set; }
    }
}
