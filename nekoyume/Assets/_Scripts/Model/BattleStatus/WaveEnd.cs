using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    public class WaveEnd : EventBase
    {
        public readonly int Wave;
        public WaveEnd(CharacterBase character, int wave) : base(character)
        {
            Wave = wave;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoWaveEnd(Wave);
        }
    }
}
