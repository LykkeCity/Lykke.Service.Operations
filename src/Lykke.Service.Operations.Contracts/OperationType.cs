namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Possible operation types
    /// </summary>
    public enum OperationType
    {
        Transfer,        
        VisaCardPayment,
        MarketOrder,
        LimitOrder,
        NewOrder,
        CashoutSwift,
        Cashout,
        StopLimitOrder
    }
}
