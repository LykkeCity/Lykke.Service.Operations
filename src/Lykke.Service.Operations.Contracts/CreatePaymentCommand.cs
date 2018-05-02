namespace Lykke.Service.Operations.Contracts
{
    public class CreatePaymentCommand
    {
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
    }
}
