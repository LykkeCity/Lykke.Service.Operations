using System;
using Lykke.Service.Operations.Contracts.SwiftCashout;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Commands
{
    /// <summary>
    /// Command to create swift cashout
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CreateSwiftCashoutCommand
    {
        public Guid OperationId { get; set; }
        public SwiftCashoutAssetModel Asset { get; set; }
        public decimal Volume { get; set; }
        public SwiftCashoutClientModel Client { get; set; }
        public SwiftFieldsModel Swift { get; set; }
        public SwiftCashoutSettingsModel CashoutSettings { get; set; }
    }
}
