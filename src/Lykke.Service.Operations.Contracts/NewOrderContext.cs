namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Information stored for a new order
    /// </summary>
    public class NewOrderContext
    {
        /// <summary>
        /// A client order id
        /// </summary>
        public string ClientOrderId { get; set; }
    }
}
