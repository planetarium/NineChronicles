using Libplanet.Crypto;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class GameConfigStateSubject
    {
        public static readonly Subject<GameConfigState> GameConfigState = new();

        public static readonly ReactiveDictionary<Address, bool> ActionPointState = new();

        public static void OnNext(GameConfigState state)
        {
            GameConfigState.OnNext(state);
        }
    }
}
