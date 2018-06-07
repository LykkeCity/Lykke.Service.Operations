using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Operations.Contracts
{
    public class SetPaymentClientIdCommand
    {
        public Guid ClientId { set; get; }
    }
}
