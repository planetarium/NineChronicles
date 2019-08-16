using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public static class SkillFactory
    {
        public static Skill Get(decimal chance, SkillEffect effect, Data.Table.Elemental.ElementalType elemental, int value)
        {
            switch (effect.type)
            {
                case SkillEffect.SkillType.Attack:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Enemy:
                            switch (effect.category)
                            {
                                case SkillEffect.Category.Normal:
                                    return new NormalAttack(chance, effect, elemental, value);
                                case SkillEffect.Category.Double:
                                    return new DoubleAttack(chance, effect, elemental, value);
                                case SkillEffect.Category.Blow:
                                    return new BlowAttack(chance, effect, elemental, value);
                                default:
                                    return new NormalAttack(chance, effect, elemental, value);
                            }
                        case SkillEffect.Target.Enemies:
                            return new AreaAttack(chance, effect, elemental, value);
                    }
                    break;
                case SkillEffect.SkillType.Buff:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Self:
                            return new Heal(chance, effect, value);
                    }
                    break;
                case SkillEffect.SkillType.Debuff:
                    break;
            }
            throw new InvalidActionException();
        }
    }
}
