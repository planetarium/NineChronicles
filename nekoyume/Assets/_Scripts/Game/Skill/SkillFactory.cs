using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public static class SkillFactory
    {
        public static SkillBase Get(CharacterBase caster, float chance, SkillEffect effect)
        {
            switch (effect.type)
            {
                case SkillEffect.SkillType.Attack:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Enemy:
                            return new Attack(caster, chance, effect);
                        case SkillEffect.Target.Enemies:
                            return new AreaAttack(caster, chance, effect);
                    }
                    break;
                case SkillEffect.SkillType.Buff:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Self:
                            return new Heal(caster, chance, effect);
                    }
                    break;
                case SkillEffect.SkillType.Debuff:
                    break;
            }
            throw new InvalidActionException();
        }
    }
}
