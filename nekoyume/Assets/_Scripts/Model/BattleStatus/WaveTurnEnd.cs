using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    public class WaveTurnEnd : EventBase
    {
        public readonly int Turn;
        
        public WaveTurnEnd(CharacterBase character, int turn) : base(character)
        {   
            Turn = turn;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoWaveTurnEnd(Turn);
        }
    }
}
