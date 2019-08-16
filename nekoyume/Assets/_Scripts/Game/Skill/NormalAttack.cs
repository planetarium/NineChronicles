using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public class NormalAttack : Attack
    {
        public NormalAttack(decimal chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental, int power) : base(chance, effect, elemental, power)
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
