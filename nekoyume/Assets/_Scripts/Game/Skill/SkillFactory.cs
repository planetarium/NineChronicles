using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public static class SkillFactory
    {
        public static SkillBase Get(float chance, SkillEffect effect, Data.Table.Elemental.ElementalType elemental)
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
                                    return new Attack(chance, effect, elemental);
                                case SkillEffect.Category.Double:
                                    return new DoubleAttack(chance, effect, elemental);
                                case SkillEffect.Category.Blow:
                                    return new Blow(chance, effect, elemental);
                                default:
                                    return new Attack(chance, effect, elemental);
                            }
                        case SkillEffect.Target.Enemies:
                            return new AreaAttack(chance, effect, elemental);
                    }
                    break;
                case SkillEffect.SkillType.Buff:
                    switch (effect.target)
                    {
                        case SkillEffect.Target.Self:
                            return new Heal(chance, effect);
                    }
                    break;
                case SkillEffect.SkillType.Debuff:
                    break;
            }
            throw new InvalidActionException();
        }
    }
}
