using System;

namespace Lykke.Service.Operations.Contracts.Cashout
{
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
