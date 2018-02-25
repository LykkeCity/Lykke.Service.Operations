using System.Linq;

namespace Lykke.Service.Operations.Workflow.Extensions
{
    public static class IcoSettingsExt
    {
        public static bool IsRestricted(this IcoSettings icoSettings, string countryIso3)
        {
            return countryIso3 != null && (icoSettings.RestrictedCountriesIso3?.Contains(countryIso3) ?? false);
        }
    }
}
