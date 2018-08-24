using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Limitations.Client;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class LimitationInput
    {
        public string ClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Volume { get; set; }
        public CurrencyOperationType OperationType { get; set; }
    }
}
