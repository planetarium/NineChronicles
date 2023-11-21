#nullable enable

using System.Linq;
using Nekoyume.Helper;

namespace Nekoyume.Multiplanetary
{
    // TODO: PlanetRegistry와 PlanetAccountInfos를 외부에서 셔플해서 사용하고 있는데,
    //       이 안에서 한 번만 셔플하도록 해야 할 것 같다. 이때, 둘이 같은 순서가 보장되게끔.
    //       또 첫 실행 시 PlanetSelector.DefaultPlanet 대신 셔플한 순서의 첫 번째가 선택되도록 하면 좋겠다.
    //       위의 내용 모두 PlanetSelector에 위임해야 하겠다.
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
