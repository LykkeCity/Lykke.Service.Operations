using Lykke.HttpClientGenerator;

namespace Lykke.Service.Operations.Client
{
    public class OperationsServiceClient : IOperationsClient
    {
        public IOperations Operations { get; }

        public OperationsServiceClient(IHttpClientGenerator httpClientGenerator)
        {
            Operations = httpClientGenerator.Generate<IOperations>();
        }
    }
}
