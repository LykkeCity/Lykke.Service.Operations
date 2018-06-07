﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class BalanceCheckInput
    {
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public decimal Volume { get; set; }
    }
}
