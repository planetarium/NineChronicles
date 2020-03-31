using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class CombinationSlotStatesSubject
    {
        public static readonly Subject<Dictionary<int, CombinationSlotState>> CombinationSlotStates =
            new Subject<Dictionary<int, CombinationSlotState>>();

        public static void OnNext(Dictionary<int, CombinationSlotState> slotStates)
        {
            CombinationSlotStates.OnNext(slotStates);
        }
    }
}
