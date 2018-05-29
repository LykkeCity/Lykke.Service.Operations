using System;
using System.Net;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.Operations.Settings.Assets;
using Lykke.Service.Operations.Settings.ServiceSettings;
using Lykke.Service.Operations.Settings.SlackNotifications;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Operations.Settings
{
    public class AppSettings
    {
        public OperationsSettings OperationsService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public AssetsSettings Assets { get; set; }
        public RateCalculatorSettings RateCalculatorServiceClient { get; set; }
        public BalancesSettings BalancesServiceClient { get; set; }
        public LimitationSettings LimitationServiceClient { get; set; }

        public FeeCalculatorSettings FeeCalculatorServiceClient { get; set; }
        public MatchingEngineSettings MatchingEngineClient { set; get; }
        public TransportSettings Transports { get; set; }
        public AssetDisclaimersServiceClientSettings AssetDisclaimersServiceClient { get; set; }
        public ExchangeOperationsServiceClientSettings ExchangeOperationsServiceClient { get; set; }

        public SagasRabbitMq SagasRabbitMq { get; set; }
    }

    public class TransportSettings
    {
        [AmqpCheck]
        public string ClientRabbitMqConnectionString { get; set; }
        [AmqpCheck]
        public string MeRabbitMqConnectionString { get; set; }
    }

    public class SagasRabbitMq
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }
    }

    public class FeeCalculatorSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class BalancesSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class RateCalculatorSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class LimitationSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class MatchingEngineSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
    }

    public class IpEndpointSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public IPEndPoint GetClientIpEndPoint()
        {
            if (IPAddress.TryParse(Host, out var ipAddress))
                return new IPEndPoint(ipAddress, Port);

            var addresses = Dns.GetHostAddressesAsync(Host).Result;
            return new IPEndPoint(addresses[0], Port);
        }
    }
}
