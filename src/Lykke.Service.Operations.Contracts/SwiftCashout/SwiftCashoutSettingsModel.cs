using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Operations.Contracts.SwiftCashout
{
    public class SwiftCashoutSettingsModel
    {
        public string HotwalletTargetId { get; set; }
        public string FeeTargetId { get; set; }
    }
}
