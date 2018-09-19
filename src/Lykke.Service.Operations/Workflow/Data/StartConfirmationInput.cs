namespace Lykke.Service.Operations.Workflow.Data
{
    public class StartConfirmationInput
    {
        public int MaxConfirmationAttempts { get; set; }

        public int ConfirmationAttemptsCount { get; set; }
    }
}
