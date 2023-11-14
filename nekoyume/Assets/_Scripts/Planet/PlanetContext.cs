#nullable enable

using Nekoyume.Helper;

namespace Nekoyume.Planet
{
    public class PlanetContext
    {
        public readonly CommandLineOptions CommandLineOptions;
        public bool IsSkipped;
        public string Error = string.Empty;
        public PlanetRegistry? PlanetRegistry;
        public PlanetInfo? SelectedPlanetInfo;
        public PlanetAccountInfo[]? PlanetAccountInfos;
        public PlanetAccountInfo? SelectedPlanetAccountInfo;

        // NOTE: This is not kind of planet context, it is authentication context.
        //       But we have no idea where to put this yet.
        public bool? NeedToAutoLogin;

        // NOTE: This is not kind of planet context, it is authentication context.
        //       But we have no idea where to put this yet.
        public bool? NeedToPledge;

        public bool HasError => !string.IsNullOrEmpty(Error);

        public PlanetContext(CommandLineOptions commandLineOptions)
        {
            CommandLineOptions = commandLineOptions;
        }
    }
}
