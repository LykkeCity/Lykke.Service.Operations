﻿using System.Collections.Generic;
using Lykke.Service.BlockchainCashoutPreconditionsCheck.Contract.Responses;

namespace Lykke.Service.Operations.Workflow
{
    public class BilOutput
    {
        public bool IsAllowed { get; set; }
        public IEnumerable<ValidationErrorResponse> Errors { get; set; }
    }
}
