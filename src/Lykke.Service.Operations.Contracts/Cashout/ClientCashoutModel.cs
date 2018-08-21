using System;
using MessagePack;

namespace Lykke.Service.Operations.Contracts.Cashout
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class ClientCashoutModel
    {
        public Guid Id { get; set; }
        public string BitcoinAddress { get; set; }        
        public decimal Balance { get; set; }
        public bool CashOutBlocked { get; set; }
        public bool BackupDone { get; set; }
        public string KycStatus { get; set; }
    }
}
