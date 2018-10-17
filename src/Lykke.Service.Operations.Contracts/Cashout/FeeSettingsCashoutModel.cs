using System.Collections.Generic;
using MessagePack;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Cashout
{
    [MessagePackObject(keyAsPropertyName: true)]
    [ProtoContract]
    public class FeeSettingsCashoutModel
    {
        [ProtoMember(1)]
        public Dictionary<string, string> TargetClients { get; set; }
    }
}
