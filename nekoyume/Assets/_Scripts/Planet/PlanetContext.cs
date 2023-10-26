#nullable enable

using Nekoyume.Helper;

namespace Nekoyume.Planet
{
    public class PlanetContext
    {
        public readonly CommandLineOptions CommandLineOptions;
        public bool IsSkipped;
        public string Error = string.Empty;
        public Planets? Planets;
        public PlanetInfo? CurrentPlanetInfo;

        public bool HasError => !string.IsNullOrEmpty(Error);

        public PlanetContext(CommandLineOptions commandLineOptions)
        {
            CommandLineOptions = commandLineOptions;
        }
    }
}
