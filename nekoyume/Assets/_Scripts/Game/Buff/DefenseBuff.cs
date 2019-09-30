using System;
using DecimalMath;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class DefenseBuff : Buff
    {
        public DefenseBuff(BuffSheet.Row row) : base(row)
        {
        }

        public override int Use(CharacterBase characterBase)
        {
            return (int) (characterBase.def * (1 + Data.Effect / 100f));
        }
    }
}
