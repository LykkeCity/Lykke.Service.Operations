using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Operations.Client
{
    public class OperationsServiceClientSettings
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }
    }
}
