using System;
using MessagePack;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Cashout
{
    [MessagePackObject(keyAsPropertyName: true)]
    [ProtoContract]
    public class ClientCashoutModel
    {
        [ProtoMember(1)]
        public Guid Id { get; set; }
        [ProtoMember(2)]
        public string BitcoinAddress { get; set; }
        [ProtoMember(3)]
        public decimal Balance { get; set; }
        [ProtoMember(4)]
        public bool CashOutBlocked { get; set; }
        [ProtoMember(5)]
        public bool BackupDone { get; set; }
        [ProtoMember(6)]
        public string KycStatus { get; set; }
        [ProtoMember(7)]
        public string ConfirmationType { get; set; }
    }
}
