using System.Collections.Generic;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Cashout
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class FeeSettingsCashoutModel
    {
        public Dictionary<string, string> TargetClients { get; set; }
    }
}
