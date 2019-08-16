using Nekoyume.Data.Table;
using Nekoyume.EnumType;

namespace Nekoyume.Game.Skill
{
    public static class SkillFactory
    {
        public static Skill Get(decimal chance, SkillEffect effect, Data.Table.Elemental.ElementalType elemental, int value)
        {
            switch (effect.type)
            {
                case SkillType.Attack:
                    switch (effect.skillTargetType)
                    {
                        case SkillTargetType.Enemy:
                            switch (effect.skillCategory)
                            {
                                case SkillCategory.Normal:
                                    return new NormalAttack(chance, effect, elemental, value);
                                case SkillCategory.Double:
                                    return new DoubleAttack(chance, effect, elemental, value);
                                case SkillCategory.Blow:
                                    return new BlowAttack(chance, effect, elemental, value);
                                default:
                                    return new NormalAttack(chance, effect, elemental, value);
                            }
                        case SkillTargetType.Enemies:
                            return new AreaAttack(chance, effect, elemental, value);
                    }
                    break;
                case SkillType.Buff:
                    switch (effect.skillTargetType)
                    {
                        case SkillTargetType.Self:
                            return new Heal(chance, effect, value);
                    }
                    break;
                case SkillType.Debuff:
                    break;
            }
            throw new InvalidActionException();
        }
    }
}
