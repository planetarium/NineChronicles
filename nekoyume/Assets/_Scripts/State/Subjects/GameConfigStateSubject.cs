using Libplanet;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class GameConfigStateSubject
    {
        public static readonly Subject<GameConfigState> GameConfigState =
            new Subject<GameConfigState>();

        public static readonly ReactiveDictionary<Address, bool> ActionPointState =
            new ReactiveDictionary<Address, bool>();

        public static void OnNext(GameConfigState state)
        {
            GameConfigState.OnNext(state);
        }

    }
}
