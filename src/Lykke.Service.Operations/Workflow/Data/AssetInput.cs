using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Orders;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class AssetInput
    {
        public string Id { get; set; }
        public string DisplayId { get; set; }
        public bool IsTradable { get; set; }
        public bool IsTrusted { get; set; }
        public OrderAction OrderAction { get; set; }        
    }
}
