using System;

namespace Lykke.Service.Operations.Contracts.SwiftCashout
{
    /// <summary>
    /// Swift operation model
    /// </summary>
    public class SwiftFieldsModel
    {
        public string Bic { get; set; }
        public string AccNumber { get; set; }
        public string AccName { get; set; }
        public string AccHolderAddress { get; set; }
        public string BankName { get; set; }

        [Obsolete("Unnecessary field, automatically extracted from Swift field, should be remove on next release")]
        public string AccHolderCountry { get; set; }
        public string AccHolderZipCode { get; set; }
        public string AccHolderCity { get; set; }
    }
}
