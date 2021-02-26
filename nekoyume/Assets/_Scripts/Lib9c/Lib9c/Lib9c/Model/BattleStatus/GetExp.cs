using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class GetExp : EventBase
    {
        public long Exp { get; }

        public GetExp(CharacterBase character, long exp) : base(character)
        {
            Exp = exp;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoGetExp(Exp);
        }
    }
}
