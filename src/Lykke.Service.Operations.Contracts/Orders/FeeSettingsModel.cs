using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Orders
{
    /// <summary>
    /// Fee settings model
    /// </summary>
    [ProtoContract]
    public class FeeSettingsModel
    {
        [ProtoMember(1)]
        public bool FeeEnabled { get; set; }
        [ProtoMember(2)]
        public string TargetClientId { get; set; }
    }
}
