using Lykke.Service.Operations.Contracts.SwiftCashout;

namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Command to create swift cashout
    /// </summary>
    public class CreateSwiftCashoutCommand
    {
        public SwiftCashoutAssetModel Asset { get; set; }
        public decimal Volume { get; set; }
        public SwiftCashoutClientModel Client { get; set; }
        public SwiftFieldsModel Swift { get; set; }
        public SwiftCashoutSettingsModel CashoutSettings { get; set; }
    }
}
