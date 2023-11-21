#nullable enable

using System.Linq;
using Nekoyume.Helper;

namespace Nekoyume.Multiplanetary
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
        public bool? CanSkipPlanetSelection;

        public bool HasError => !string.IsNullOrEmpty(Error);

        public bool HasAccount => PlanetAccountInfos?.Any() ?? false;

        public bool HasPledgedAccount => PlanetAccountInfos?.Any(e => e.IsAgentPledged.HasValue &&
                                                               e.IsAgentPledged.Value) ?? false;

        public bool IsSelectedPlanetAccountPledged => SelectedPlanetAccountInfo is { IsAgentPledged: true };

        public PlanetContext(CommandLineOptions commandLineOptions)
        {
            CommandLineOptions = commandLineOptions;
        }
    }
}
