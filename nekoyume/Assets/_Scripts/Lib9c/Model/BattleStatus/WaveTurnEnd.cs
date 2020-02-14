using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class WaveTurnEnd : EventBase
    {
        public readonly int WaveTurn;
        public readonly int Turn;
        public WaveTurnEnd(CharacterBase character, int waveTurn, int turn) : base(character)
        {
            WaveTurn = waveTurn;
            Turn = turn;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoWaveTurnEnd(WaveTurn, Turn);
        }
    }
}
