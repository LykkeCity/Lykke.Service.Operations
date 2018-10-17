using System;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Orders
{
    /// <summary>
    /// Client model
    /// </summary>
    [ProtoContract]
    public class ClientModel
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public bool TradesBlocked { get; set; }
        [ProtoMember(3)]
        public bool BackupDone { get; set; }
        [ProtoMember(4)]
        public string KycStatus { get; set; }
        [ProtoMember(5)]
        public PersonalDataModel PersonalData { get; set; }
    }
}
