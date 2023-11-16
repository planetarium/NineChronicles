#nullable enable

using System.Collections.Generic;
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
        public bool? NeedToTryAutoLogin;

        // NOTE: This is not kind of planet context, it is authentication context.
        //       But we have no idea where to put this yet.
        public bool? NeedToPledge;

        public List<(string eventKey, long elapsedMilliseconds, string? description)> ElapsedTuples;

        public bool HasError => !string.IsNullOrEmpty(Error);

        public PlanetContext(CommandLineOptions commandLineOptions)
        {
            CommandLineOptions = commandLineOptions;
            ElapsedTuples = new List<(string eventKey, long elapsedMilliseconds, string? description)>();
        }
    }
}
