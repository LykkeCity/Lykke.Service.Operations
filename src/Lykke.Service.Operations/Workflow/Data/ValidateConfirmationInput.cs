using System;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class ValidateConfirmationInput
    {
        public Guid OperationId { get; set; }
        public string ClientId { get; set; }
        public string Confirmation { get; set; }
        public string ConfirmationType { get; set; }
    }
}
