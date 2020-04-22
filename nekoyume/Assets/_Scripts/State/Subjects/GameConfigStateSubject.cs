using System.Collections.Generic;
using Nekoyume.Model.State;
using UniRx;

namespace Nekoyume.State.Subjects
{
    public class GameConfigStateSubject
    {
        public static readonly Subject<GameConfigState> gameConfigState =
            new Subject<GameConfigState>();

        public static void OnNext(GameConfigState state)
        {
            gameConfigState.OnNext(state);
        }

    }
}
