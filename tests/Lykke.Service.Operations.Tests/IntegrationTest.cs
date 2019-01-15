using System;
using System.Threading.Tasks;
using Lykke.HttpClientGenerator.Infrastructure;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts.Commands;
using Xunit;

namespace Lykke.Service.Operations.Tests
{
    public class IntegrationTest
    {
        private readonly string _url = "http://localhost:5000";

        [Fact(Skip="integration test")]
        public async Task CallApiByOldClient()
        {
            var httpClientGenerator = HttpClientGenerator.HttpClientGenerator
                .BuildForUrl(_url)
                .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper())
                .WithoutRetries()
                .Create();
            var oc = new OperationsServiceClient(httpClientGenerator);
            var id = Guid.NewGuid();

            var orderId = await oc.Operations.NewOrder(id, new CreateNewOrderCommand
            {
                WalletId = Guid.NewGuid(),
                ClientOrderId = Guid.NewGuid().ToString()
            });

            Assert.Equal(orderId, id);

            var order = await oc.Operations.Get(id);

            Assert.NotNull(order);
            Assert.Equal(id, order.Id);
        }
    }
}
