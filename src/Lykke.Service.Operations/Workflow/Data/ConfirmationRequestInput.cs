using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class ConfirmationRequestInput
    {
        public Guid OperationId { get; set; }

        public Guid ClientId { get; set; }

        public string ConfirmationType { get; set; }
    }
}
