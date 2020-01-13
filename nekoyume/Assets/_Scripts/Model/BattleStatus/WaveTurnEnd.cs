using System.Collections;

namespace Nekoyume.Model
{
    public class WaveTurnEnd : EventBase
    {
        public readonly int Turn;
        public WaveTurnEnd(CharacterBase character, int waveTurn) : base(character)
        {
            Turn = waveTurn;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoWaveTurnEnd(Turn);
        }
    }
}
