using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class SwiftInput
    {
        public string Bic { get; set; }
        public string AccNumber { get; set; }
        public string AccName { get; set; }
        public string AccHolderAddress { get; set; }
        public string BankName { get; set; }

        public string AccHolderZipCode { get; set; }
        public string AccHolderCity { get; set; }
    }

    public static class SwiftInputExtensions
    {
        public static string GetCountryCode(this string bic)
        {
            if (string.IsNullOrWhiteSpace(bic))
                return null;
            return bic.Length >= 6 ? bic.Substring(4, 2).ToUpperInvariant() : null;
        }
    }
}
