namespace Lykke.Service.Operations.Contracts
{
    public class CreateLimitOrderCommand : CreateOrderCommand
    {
        public decimal Price { get; set; }            
    }
}
