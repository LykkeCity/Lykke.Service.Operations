// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Operations.Client.AutorestClient.Models
{
    using Lykke.Service;
    using Lykke.Service.Operations;
    using Lykke.Service.Operations.Client;
    using Lykke.Service.Operations.Client.AutorestClient;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Runtime;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines values for ModelValidationState.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModelValidationState
    {
        [EnumMember(Value = "Unvalidated")]
        Unvalidated,
        [EnumMember(Value = "Invalid")]
        Invalid,
        [EnumMember(Value = "Valid")]
        Valid,
        [EnumMember(Value = "Skipped")]
        Skipped
    }
    internal static class ModelValidationStateEnumExtension
    {
        internal static string ToSerializedValue(this ModelValidationState? value)  =>
            value == null ? null : ((ModelValidationState)value).ToSerializedValue();

        internal static string ToSerializedValue(this ModelValidationState value)
        {
            switch( value )
            {
                case ModelValidationState.Unvalidated:
                    return "Unvalidated";
                case ModelValidationState.Invalid:
                    return "Invalid";
                case ModelValidationState.Valid:
                    return "Valid";
                case ModelValidationState.Skipped:
                    return "Skipped";
            }
            return null;
        }

        internal static ModelValidationState? ParseModelValidationState(this string value)
        {
            switch( value )
            {
                case "Unvalidated":
                    return ModelValidationState.Unvalidated;
                case "Invalid":
                    return ModelValidationState.Invalid;
                case "Valid":
                    return ModelValidationState.Valid;
                case "Skipped":
                    return ModelValidationState.Skipped;
            }
            return null;
        }
    }
}