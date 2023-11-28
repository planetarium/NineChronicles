using Nekoyume.L10n;

namespace Nekoyume.Multiplanetary.Extensions
{
    public static class PlanetIdExtensions
    {
        public static string ToLocalizedPlanetName(
            this PlanetId planetId,
            bool containsPlanetId)
        {
            var planetIdString = planetId.ToString();
            return ToLocalizedPlanetName(planetIdString, containsPlanetId);
        }

        public static string ToLocalizedPlanetName(
            string planetIdString,
            bool containsPlanetId)
        {
            if (!planetIdString.StartsWith("0x"))
            {
                planetIdString = $"0x{planetIdString}";
            }

            var key = $"PLANET_{planetIdString}";
            var localized = L10nManager.Localize(key);
            return containsPlanetId
                ? $"{localized} ({planetIdString})"
                : localized;
        }
    }
}
