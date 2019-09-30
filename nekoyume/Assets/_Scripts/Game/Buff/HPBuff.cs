using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class HPBuff: Buff
    {
        public HPBuff(BuffSheet.Row row) : base(row)
        {
        }

        public override int Use(CharacterBase characterBase)
        {
            return (int) (characterBase.hp * (1 + Data.Effect / 100f)); 
        }
    }
}
