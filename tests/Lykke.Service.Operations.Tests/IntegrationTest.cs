using System;
using System.Threading.Tasks;
using Lykke.Service.Operations.Contracts;
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
            var oc = new Client.OperationsClient(_url);
            var id = Guid.NewGuid();
            await oc.NewOrder(id, new CreateNewOrderCommand
            {
                WalletId = Guid.NewGuid(),
                ClientOrderId = Guid.NewGuid().ToString()
            });

            var res = await oc.Get(id);
        }
    }
}
