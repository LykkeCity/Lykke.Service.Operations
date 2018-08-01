                    using Lykke.Service.FeeCalculator.AutorestClient.Models;

    namespace Lykke.Service.Operations.Workflow
{
    public class CashoutMeInput
    {
        public string OperationId { get; set; } 
        public string ClientId { get; set; }
        public string DestinationAddress { get; set; }
        public decimal Volume { get; set; }
        public string AssetId { get; set; }        
        public double FeeSize { get; set; }
        public FeeType FeeType { get; set; }
        public string CashoutTargetClientId { get; set; }
    }
}
