using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class AreaAttack: Attack
    {
        public AreaAttack(SkillSheet.Row skillRow, int power, decimal chance) : base(skillRow, power, chance)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            ProcessBuff(caster);
            return new Model.AreaAttack
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = ProcessDamage(caster),
            };
        }
    }
}
