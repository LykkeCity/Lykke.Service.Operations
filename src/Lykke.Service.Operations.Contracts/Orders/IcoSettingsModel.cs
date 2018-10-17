using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Orders
{
    /// <summary>
    /// Ico settings model
    /// </summary>
    [ProtoContract]
    public class IcoSettingsModel
    {
        [ProtoMember(1)]
        public string[] RestrictedCountriesIso3 { get; set; }
        [ProtoMember(2)]
        public string LKK2YAssetId { get; set; }
    }
}
