namespace Lykke.Service.Operations.Contracts
{
    public class AssetModel
    {
        public string Id { get; set; }
        public int Accuracy { get; set; }
        public bool IsTradable { get; set; }
        public bool IsTrusted { get; set; }
        public bool KycNeeded { get; set; }
        public string LykkeEntityId { get; set; }
        public string Blockchain { get; set; }        
    }

    public class AssetShortModel
    {
        public string Id { get; set; }        
        public bool IsTradable { get; set; }
        public bool IsTrusted { get; set; }        
        public string Blockchain { get; set; }
    }
}
