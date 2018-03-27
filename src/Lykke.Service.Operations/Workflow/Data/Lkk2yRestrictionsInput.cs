using Lykke.Service.Operations.Workflow.Extensions;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class Lkk2yRestrictionsInput
    {
        public string CountryFromPOA { get; set; }        
        public string BaseAssetId { get; set; }
        public string QuotingAssetId { get; set; }
        public IcoSettings IcoSettings { get; set; }        
    }
}
