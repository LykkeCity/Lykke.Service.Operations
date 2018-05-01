namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Fee settings model
    /// </summary>
    public class FeeSettingsModel
    {
        public bool FeeEnabled { get; set; }
        public string TargetClientId { get; set; }
    }
}
