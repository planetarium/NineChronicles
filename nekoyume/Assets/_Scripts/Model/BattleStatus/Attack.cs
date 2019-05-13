using System;
using System.Collections;

namespace Nekoyume.Model
{
    [Serializable]
    public class Attack : EventBase
    {
        public AttackInfo info;

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoAttack(character, info);
        }

        [Serializable]
        public class AttackInfo
        {
            public CharacterBase target;
            public int damage;
            public bool critical;

            public AttackInfo(CharacterBase character, int dmg, bool cri)
            {
                target = character;
                damage = dmg;
                critical = cri;
            }
        }
    }
}
