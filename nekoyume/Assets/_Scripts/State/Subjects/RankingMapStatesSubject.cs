using System.Collections.Generic;
using Libplanet;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class RankingMapStatesSubject
    {
        public static readonly Subject<Dictionary<Address, RankingMapState>> RankingMapStates
            = new Subject<Dictionary<Address,RankingMapState>>();

        public static void OnNext(Dictionary<Address, RankingMapState> states)
        {
            RankingMapStates.OnNext(states);
        }
    }
}
