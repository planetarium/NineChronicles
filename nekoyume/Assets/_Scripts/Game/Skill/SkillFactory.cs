using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public static class SkillFactory
    {
        public static SkillBase Get(CharacterBase caster, SkillEffect effect)
        {
            switch (effect.type)
            {
                case SkillEffect.SkillType.Attack:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Enemy:
                            return new Attack(caster, effect);
                        case SkillEffect.Target.Enemies:
                            return new AreaAttack(caster, effect);
                    }
                    break;
                case SkillEffect.SkillType.Buff:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Self:
                            return new Heal(caster, effect);
                    }
                    break;
                case SkillEffect.SkillType.Debuff:
                    break;
            }
            throw new InvalidActionException();
        }
    }
}
