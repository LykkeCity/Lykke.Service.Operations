using System;

namespace Lykke.Service.Operations.Contracts.SwiftCashout
{
    /// <summary>
    /// Client model
    /// </summary>
    public class SwiftCashoutClientModel
    {
        public Guid Id { get; set; }
        public string KycStatus { get; set; }
    }
}
