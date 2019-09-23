using System;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Buff
{
    [Serializable]
    public class DefenseBuff : Buff
    {
        public DefenseBuff(BuffSheet.Row row) : base(row)
        {
        }

        public override BuffCategory Category => BuffCategory.Defense;
        public override int Use(CharacterBase characterBase)
        {
            return characterBase.def * (100 + effect) / 100;
        }
    }
}
