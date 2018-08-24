namespace Lykke.Service.Operations.Workflow.Data
{
    public class AjustmentInput
    {
        public string AssetId { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal CashoutMinimalAmount { get; set; }
        public decimal Balance { get; set; }
        public decimal Volume { get; set; }        
    }
}
