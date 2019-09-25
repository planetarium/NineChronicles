using System;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Buff
{
    [Serializable]
    public class AttackBuff : Buff
    {
        public override BuffCategory Category => BuffCategory.Attack;
        public override int Use(CharacterBase characterBase)
        {
            return characterBase.atk * (100 + effect) / 100;
        }

        public AttackBuff(BuffSheet.Row row) : base(row)
        {
        }
    }
}
