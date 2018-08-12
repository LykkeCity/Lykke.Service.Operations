namespace Lykke.Service.Operations.Workflow
{
    public class ValidateConfirmationInput
    {
        public string ClientId { get; set; }
        public string PubKeyAddress { get; set; }
        public string Challenge { get; set; }
        public string SignedMessage { get; set; }
    }
}
