using System.Net;
using Lykke.Service.Operations.Core.Settings.Assets;
using Lykke.Service.Operations.Core.Settings.ServiceSettings;
using Lykke.Service.Operations.Core.Settings.SlackNotifications;

namespace Lykke.Service.Operations.Core.Settings
{
    public class AppSettings
    {
        public OperationsSettings OperationsService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public AssetsSettings Assets { get; set; }
        public RateCalculatorSettings RateCalculatorServiceClient { get; set; }
        public BalancesSettings BalancesServiceClient { get; set; }

        public FeeCalculatorSettings FeeCalculatorServiceClient { get; set; }
        public MatchingEngineSettings MatchingEngineClient { set; get; }
    }

    public class FeeCalculatorSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class BalancesSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class RateCalculatorSettings
    {
        public string ServiceUrl { get; set; }
    }

    public class MatchingEngineSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
    }

    public class IpEndpointSettings
    {
        public string InternalHost { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public IPEndPoint GetClientIpEndPoint(bool useInternal = false)
        {
            string host = useInternal ? InternalHost : Host;

            if (IPAddress.TryParse(host, out var ipAddress))
                return new IPEndPoint(ipAddress, Port);

            var addresses = Dns.GetHostAddressesAsync(host).Result;
            return new IPEndPoint(addresses[0], Port);
        }
    }
}
