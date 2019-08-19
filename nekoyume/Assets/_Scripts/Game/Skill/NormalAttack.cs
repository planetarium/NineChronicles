using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class NormalAttack : Attack
    {
        public NormalAttack(SkillSheet.Row skillRow, int power, decimal chance) : base(skillRow, power, chance)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var info = ProcessDamage(caster);

            return new Model.Attack
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
