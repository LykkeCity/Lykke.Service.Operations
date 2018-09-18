namespace Lykke.Service.Operations.Workflow.Data
{
    public class ValidateConfirmationInput
    {
        public string ClientId { get; set; }
        public string PubKeyAddress { get; set; }
        public string Challenge { get; set; }
        public string Confirmation { get; set; }
        public string ConfirmationType { get; set; }
    }
}
