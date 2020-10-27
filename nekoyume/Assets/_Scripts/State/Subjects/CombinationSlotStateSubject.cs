using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class CombinationSlotStateSubject
    {
        public static readonly Subject<CombinationSlotState> CombinationSlotState =
            new Subject<CombinationSlotState>();

        public static void OnNext(CombinationSlotState slotState)
        {
            CombinationSlotState.OnNext(slotState);
        }
    }
}
