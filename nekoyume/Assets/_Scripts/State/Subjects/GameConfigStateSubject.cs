using System.Collections.Generic;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public static class GameConfigStateSubject
    {
        public static readonly Subject<GameConfigState> GameConfigState =
            new Subject<GameConfigState>();

        public static void OnNext(GameConfigState state)
        {
            GameConfigState.OnNext(state);
        }

    }
}
