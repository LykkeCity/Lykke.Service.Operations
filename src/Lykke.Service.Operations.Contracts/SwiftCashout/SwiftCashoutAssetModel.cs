namespace Lykke.Service.Operations.Contracts.SwiftCashout
{
    /// <summary>
    /// Asset model
    /// </summary>
    public class SwiftCashoutAssetModel
    {
        public string Id { get; set; }
        public bool KycNeeded { get; set; }
        public bool SwiftCashoutEnabled { get; set; }
        public string LykkeEntityId { get; set; }
    }
}
