using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class CombinationSlotStatesSubject
    {
        public static readonly Subject<Dictionary<Address, CombinationSlotState>> CombinationSlotStates =
            new Subject<Dictionary<Address, CombinationSlotState>>();

        public static void OnNext(Dictionary<Address, CombinationSlotState> slotStates)
        {
            CombinationSlotStates.OnNext(slotStates);
        }
    }
}
