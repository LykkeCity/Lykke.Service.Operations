using System;

namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Client model
    /// </summary>
    public class ClientModel
    {
        public Guid Id { get; set; }
        public bool TradesBlocked { get; set; }
        public bool BackupDone { get; set; }
        public string KycStatus { get; set; }
        public PersonalDataModel PersonalData { get; set; }
    }
}
