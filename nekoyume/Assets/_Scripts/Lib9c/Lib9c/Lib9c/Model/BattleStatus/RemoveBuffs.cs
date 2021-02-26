using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class RemoveBuffs : EventBase
    {
        public RemoveBuffs(CharacterBase character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoRemoveBuffs(Character);
        }
    }
}
